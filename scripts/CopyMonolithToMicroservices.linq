<Query Kind="Program">
  <NuGetReference>Microsoft.Data.SqlClient</NuGetReference>
  <Namespace>Microsoft.Data.SqlClient</Namespace>
</Query>

// =============================================================================
// Copy Companies & Jobs from Monolith → Microservices
// =============================================================================
// Reads from the monolith database and inserts into:
//   - company-api: Industries, Companies
//   - job-api:     Companies (slim), Jobs, Responsibilities, Qualifications
//   - user-api:    Companies (with KeycloakGroupId)
//
// Monolith naming:  InternalId (int PK) + Id (Guid public)
// Micro naming:     Id (int PK, IDENTITY) + UId (Guid public)
//
// Monolith has NO RecordStatus column; micros require RecordStatus = 'Active'.
// Monolith stores JobType as int; job-api stores it as nvarchar string.
// =============================================================================

// ── Connection Strings ── plug these in ──────────────────────────────────────
const string MonolithConn  = "Server=localhost;Database=JobBoard;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";
const string MicroConn     = "Server=localhost;Database=Job-board;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";
// All microservices share the same DB with different schemas:
//   company-api → [Company], job-api → [Jobs], user-api → [Users]

void Main()
{
	// 1. Read from monolith
	var industries = ReadMonolithIndustries();
	$"Found {industries.Count} industries".Dump("Step 1");

	var companies = ReadMonolithCompanies();
	$"Found {companies.Count} companies".Dump("Step 2");

	var jobs = ReadMonolithJobs();
	$"Found {jobs.Count} jobs".Dump("Step 3");

	// 2. Insert into company-api
	var industryMap = UpsertIndustries(industries);
	industryMap.Dump("Step 4 — Industry Guid → Micro Id");

	var companyMap = UpsertCompanies(companies, industryMap);
	companyMap.Dump("Step 5 — Company Guid → Micro Company Id");

	// 3. Insert into job-api (companies + jobs)
	var jobApiCompanyMap = UpsertJobApiCompanies(companies);
	jobApiCompanyMap.Dump("Step 6 — Company Guid → Job-API Company Id");

	var jobsInserted = UpsertJobs(jobs, companies, jobApiCompanyMap);
	$"Inserted {jobsInserted} jobs with responsibilities & qualifications".Dump("Step 7");

	// 4. Insert into user-api
	var userApiInserted = UpsertUserApiCompanies(companies);
	$"Inserted {userApiInserted} companies into user-api".Dump("Step 8 — Done");
}

// ── Data Models ──────────────────────────────────────────────────────────────

record MonolithIndustry(int InternalId, Guid Id, string Name, DateTime CreatedAt, string CreatedBy, DateTime UpdatedAt, string UpdatedBy);

record MonolithCompany(
	int InternalId, Guid Id, string Name, string Email, string Status,
	string? Description, string? Website, string? Phone, string? About,
	string? EEO, DateTime? Founded, string? Size, string? Logo,
	int IndustryId, Guid IndustryGuid,
	DateTime CreatedAt, string CreatedBy, DateTime UpdatedAt, string UpdatedBy
);

record MonolithJob(
	int InternalId, Guid Id, string Title, string Location, string AboutRole,
	string? SalaryRange, int JobType, int CompanyId, Guid CompanyGuid,
	DateTime CreatedAt, string CreatedBy, DateTime UpdatedAt, string UpdatedBy,
	List<MonolithChild> Responsibilities, List<MonolithChild> Qualifications
);

record MonolithChild(Guid Id, string Value);

// ── Readers (Monolith) ──────────────────────────────────────────────────────

List<MonolithIndustry> ReadMonolithIndustries()
{
	var list = new List<MonolithIndustry>();
	using var conn = new SqlConnection(MonolithConn);
	conn.Open();
	using var cmd = new SqlCommand(
		"SELECT InternalId, Id, Name, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy FROM [Company].[Industries]", conn);
	using var r = cmd.ExecuteReader();
	while (r.Read())
		list.Add(new(r.GetInt32(0), r.GetGuid(1), r.GetString(2),
			r.GetDateTime(3), r.GetString(4), r.GetDateTime(5), r.GetString(6)));
	return list;
}

List<MonolithCompany> ReadMonolithCompanies()
{
	var list = new List<MonolithCompany>();
	using var conn = new SqlConnection(MonolithConn);
	conn.Open();
	using var cmd = new SqlCommand(@"
		SELECT c.InternalId, c.Id, c.Name, c.Email, c.Status,
		       c.Description, c.Website, c.Phone, c.About, c.EEO,
		       c.Founded, c.Size, c.Logo, c.IndustryId, i.Id AS IndustryGuid,
		       c.CreatedAt, c.CreatedBy, c.UpdatedAt, c.UpdatedBy
		FROM [Company].[Companies] c
		JOIN [Company].[Industries] i ON c.IndustryId = i.InternalId", conn);
	using var r = cmd.ExecuteReader();
	while (r.Read())
	{
		list.Add(new(
			r.GetInt32(0), r.GetGuid(1), r.GetString(2), r.GetString(3), r.GetString(4),
			r.IsDBNull(5) ? null : r.GetString(5),
			r.IsDBNull(6) ? null : r.GetString(6),
			r.IsDBNull(7) ? null : r.GetString(7),
			r.IsDBNull(8) ? null : r.GetString(8),
			r.IsDBNull(9) ? null : r.GetString(9),
			r.IsDBNull(10) ? null : r.GetDateTime(10),
			r.IsDBNull(11) ? null : r.GetString(11),
			r.IsDBNull(12) ? null : r.GetString(12),
			r.GetInt32(13), r.GetGuid(14),
			r.GetDateTime(15), r.GetString(16), r.GetDateTime(17), r.GetString(18)
		));
	}
	return list;
}

List<MonolithJob> ReadMonolithJobs()
{
	var jobs = new List<MonolithJob>();
	using var conn = new SqlConnection(MonolithConn);
	conn.Open();

	using (var cmd = new SqlCommand(@"
		SELECT j.InternalId, j.Id, j.Title, j.Location, j.AboutRole, j.SalaryRange,
		       j.JobType, j.CompanyId, c.Id AS CompanyGuid,
		       j.CreatedAt, j.CreatedBy, j.UpdatedAt, j.UpdatedBy
		FROM [Job].[Jobs] j
		JOIN [Company].[Companies] c ON j.CompanyId = c.InternalId", conn))
	using (var r = cmd.ExecuteReader())
	{
		while (r.Read())
		{
			jobs.Add(new(
				r.GetInt32(0), r.GetGuid(1), r.GetString(2), r.GetString(3),
				r.GetString(4), r.IsDBNull(5) ? null : r.GetString(5),
				r.GetInt32(6), r.GetInt32(7), r.GetGuid(8),
				r.GetDateTime(9), r.GetString(10), r.GetDateTime(11), r.GetString(12),
				new List<MonolithChild>(), new List<MonolithChild>()
			));
		}
	}

	// Read responsibilities
	var respByJob = ReadMonolithChildren(conn, "[Job].[Responsibilities]");
	var qualByJob = ReadMonolithChildren(conn, "[Job].[Qualifications]");

	foreach (var job in jobs)
	{
		if (respByJob.TryGetValue(job.InternalId, out var resps))
			job.Responsibilities.AddRange(resps);
		if (qualByJob.TryGetValue(job.InternalId, out var quals))
			job.Qualifications.AddRange(quals);
	}
	return jobs;
}

Dictionary<int, List<MonolithChild>> ReadMonolithChildren(SqlConnection conn, string table)
{
	var dict = new Dictionary<int, List<MonolithChild>>();
	using var cmd = new SqlCommand(
		$"SELECT JobId, Id, Value FROM {table}", conn);
	using var r = cmd.ExecuteReader();
	while (r.Read())
	{
		int jobId = r.GetInt32(0);
		if (!dict.ContainsKey(jobId)) dict[jobId] = new();
		dict[jobId].Add(new(r.GetGuid(1), r.GetString(2)));
	}
	return dict;
}

// ── Writers (Microservices) ─────────────────────────────────────────────────

// ── company-api: [Company].[Industries] ──
Dictionary<Guid, int> UpsertIndustries(List<MonolithIndustry> industries)
{
	var map = new Dictionary<Guid, int>();
	using var conn = new SqlConnection(MicroConn);
	conn.Open();

	foreach (var ind in industries)
	{
		// Check by UId
		using (var chk = new SqlCommand(
			"SELECT Id FROM [Company].[Industries] WHERE UId = @uid", conn))
		{
			chk.Parameters.AddWithValue("@uid", ind.Id);
			var existing = chk.ExecuteScalar();
			if (existing != null)
			{
				map[ind.Id] = (int)existing;
				$"Industry '{ind.Name}' already exists, skipping".Dump();
				continue;
			}
		}

		// Also check by Name (case-insensitive match)
		using (var chk = new SqlCommand(
			"SELECT Id, UId FROM [Company].[Industries] WHERE Name = @name", conn))
		{
			chk.Parameters.AddWithValue("@name", ind.Name);
			using var r = chk.ExecuteReader();
			if (r.Read())
			{
				int existingId = r.GetInt32(0);
				map[ind.Id] = existingId;
				$"Industry '{ind.Name}' matched by name (micro Id={existingId}), skipping".Dump();
				continue;
			}
		}

		using var cmd = new SqlCommand(@"
			SET IDENTITY_INSERT [Company].[Industries] OFF;
			INSERT INTO [Company].[Industries]
				(UId, Name, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RecordStatus)
			VALUES
				(@uid, @name, @createdAt, @createdBy, @updatedAt, @updatedBy, 'Active');
			SELECT SCOPE_IDENTITY();", conn);

		cmd.Parameters.AddWithValue("@uid", ind.Id);
		cmd.Parameters.AddWithValue("@name", ind.Name);
		cmd.Parameters.AddWithValue("@createdAt", ind.CreatedAt);
		cmd.Parameters.AddWithValue("@createdBy", Truncate(ind.CreatedBy, 50));
		cmd.Parameters.AddWithValue("@updatedAt", ind.UpdatedAt);
		cmd.Parameters.AddWithValue("@updatedBy", Truncate(ind.UpdatedBy, 50));

		int newId = Convert.ToInt32(cmd.ExecuteScalar());
		map[ind.Id] = newId;
		$"Inserted industry '{ind.Name}' → Id={newId}".Dump();
	}
	return map;
}

// ── company-api: [Company].[Companies] ──
Dictionary<Guid, int> UpsertCompanies(List<MonolithCompany> companies, Dictionary<Guid, int> industryMap)
{
	var map = new Dictionary<Guid, int>();
	using var conn = new SqlConnection(MicroConn);
	conn.Open();

	foreach (var c in companies)
	{
		// Check if already exists by UId
		using (var chk = new SqlCommand(
			"SELECT Id FROM [Company].[Companies] WHERE UId = @uid", conn))
		{
			chk.Parameters.AddWithValue("@uid", c.Id);
			var existing = chk.ExecuteScalar();
			if (existing != null)
			{
				map[c.Id] = (int)existing;
				$"Company '{c.Name}' already exists in company-api, skipping".Dump();
				continue;
			}
		}

		if (!industryMap.TryGetValue(c.IndustryGuid, out int microIndustryId))
		{
			$"SKIPPED company '{c.Name}' — industry guid {c.IndustryGuid} not mapped".Dump();
			continue;
		}

		using var cmd = new SqlCommand(@"
			INSERT INTO [Company].[Companies]
				(UId, Name, Email, Status, Description, Website, Phone, About, EEO,
				 Founded, Size, Logo, IndustryId,
				 CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RecordStatus)
			VALUES
				(@uid, @name, @email, @status, @desc, @website, @phone, @about, @eeo,
				 @founded, @size, @logo, @industryId,
				 @createdAt, @createdBy, @updatedAt, @updatedBy, 'Active');
			SELECT SCOPE_IDENTITY();", conn);

		cmd.Parameters.AddWithValue("@uid", c.Id);
		cmd.Parameters.AddWithValue("@name", c.Name);
		cmd.Parameters.AddWithValue("@email", c.Email);
		cmd.Parameters.AddWithValue("@status", c.Status);
		cmd.Parameters.AddWithValue("@desc", (object?)c.Description ?? DBNull.Value);
		cmd.Parameters.AddWithValue("@website", (object?)c.Website ?? DBNull.Value);
		cmd.Parameters.AddWithValue("@phone", (object?)c.Phone ?? DBNull.Value);
		cmd.Parameters.AddWithValue("@about", (object?)c.About ?? DBNull.Value);
		cmd.Parameters.AddWithValue("@eeo", (object?)c.EEO ?? DBNull.Value);
		cmd.Parameters.AddWithValue("@founded", (object?)c.Founded ?? DBNull.Value);
		cmd.Parameters.AddWithValue("@size", (object?)c.Size ?? DBNull.Value);
		cmd.Parameters.AddWithValue("@logo", (object?)c.Logo ?? DBNull.Value);
		cmd.Parameters.AddWithValue("@industryId", microIndustryId);
		cmd.Parameters.AddWithValue("@createdAt", c.CreatedAt);
		cmd.Parameters.AddWithValue("@createdBy", Truncate(c.CreatedBy, 50));
		cmd.Parameters.AddWithValue("@updatedAt", c.UpdatedAt);
		cmd.Parameters.AddWithValue("@updatedBy", Truncate(c.UpdatedBy, 50));

		int newId = Convert.ToInt32(cmd.ExecuteScalar());
		map[c.Id] = newId;
		$"Inserted company '{c.Name}' into company-api → Id={newId}".Dump();
	}
	return map;
}

// ── job-api: [Jobs].[Companies] (slim — just Name + UId) ──
Dictionary<Guid, int> UpsertJobApiCompanies(List<MonolithCompany> companies)
{
	var map = new Dictionary<Guid, int>();
	using var conn = new SqlConnection(MicroConn);
	conn.Open();

	foreach (var c in companies)
	{
		using (var chk = new SqlCommand(
			"SELECT Id FROM [Jobs].[Companies] WHERE UId = @uid", conn))
		{
			chk.Parameters.AddWithValue("@uid", c.Id);
			var existing = chk.ExecuteScalar();
			if (existing != null)
			{
				map[c.Id] = (int)existing;
				$"Company '{c.Name}' already exists in job-api, skipping".Dump();
				continue;
			}
		}

		using var cmd = new SqlCommand(@"
			INSERT INTO [Jobs].[Companies]
				(UId, Name, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RecordStatus)
			VALUES
				(@uid, @name, @createdAt, @createdBy, @updatedAt, @updatedBy, 'Active');
			SELECT SCOPE_IDENTITY();", conn);

		cmd.Parameters.AddWithValue("@uid", c.Id);
		cmd.Parameters.AddWithValue("@name", c.Name);
		cmd.Parameters.AddWithValue("@createdAt", c.CreatedAt);
		cmd.Parameters.AddWithValue("@createdBy", Truncate(c.CreatedBy, 50));
		cmd.Parameters.AddWithValue("@updatedAt", c.UpdatedAt);
		cmd.Parameters.AddWithValue("@updatedBy", Truncate(c.UpdatedBy, 50));

		int newId = Convert.ToInt32(cmd.ExecuteScalar());
		map[c.Id] = newId;
		$"Inserted company '{c.Name}' into job-api → Id={newId}".Dump();
	}
	return map;
}

// ── job-api: [Jobs].[Jobs], [Jobs].[Responsibilities], [Jobs].[Qualifications] ──
int UpsertJobs(List<MonolithJob> jobs, List<MonolithCompany> companies, Dictionary<Guid, int> jobApiCompanyMap)
{
	int count = 0;
	using var conn = new SqlConnection(MicroConn);
	conn.Open();

	foreach (var j in jobs)
	{
		if (!jobApiCompanyMap.TryGetValue(j.CompanyGuid, out int microCompanyId))
		{
			$"SKIPPED job '{j.Title}' — company guid {j.CompanyGuid} has no job-api mapping".Dump();
			continue;
		}

		// Check if job already exists
		using (var chk = new SqlCommand(
			"SELECT Id FROM [Jobs].[Jobs] WHERE UId = @uid", conn))
		{
			chk.Parameters.AddWithValue("@uid", j.Id);
			if (chk.ExecuteScalar() != null)
			{
				$"Job '{j.Title}' already exists in job-api, skipping".Dump();
				continue;
			}
		}

		// Convert int enum → string for job-api
		string jobTypeStr = j.JobType switch
		{
			0 => "FullTime",
			1 => "PartTime",
			2 => "Contract",
			3 => "Internship",
			_ => throw new Exception($"Unknown JobType int: {j.JobType}")
		};

		int newJobId;
		using (var cmd = new SqlCommand(@"
			INSERT INTO [Jobs].[Jobs]
				(UId, Title, Location, AboutRole, SalaryRange, JobType,
				 CompanyId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RecordStatus)
			VALUES
				(@uid, @title, @location, @aboutRole, @salaryRange, @jobType,
				 @companyId, @createdAt, @createdBy, @updatedAt, @updatedBy, 'Active');
			SELECT SCOPE_IDENTITY();", conn))
		{
			cmd.Parameters.AddWithValue("@uid", j.Id);
			cmd.Parameters.AddWithValue("@title", j.Title);
			cmd.Parameters.AddWithValue("@location", j.Location);
			cmd.Parameters.AddWithValue("@aboutRole", j.AboutRole);
			cmd.Parameters.AddWithValue("@salaryRange", (object?)j.SalaryRange ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@jobType", jobTypeStr);
			cmd.Parameters.AddWithValue("@companyId", microCompanyId);
			cmd.Parameters.AddWithValue("@createdAt", j.CreatedAt);
			cmd.Parameters.AddWithValue("@createdBy", Truncate(j.CreatedBy, 50));
			cmd.Parameters.AddWithValue("@updatedAt", j.UpdatedAt);
			cmd.Parameters.AddWithValue("@updatedBy", Truncate(j.UpdatedBy, 50));
			newJobId = Convert.ToInt32(cmd.ExecuteScalar());
		}

		// Insert responsibilities
		foreach (var resp in j.Responsibilities)
		{
			using var cmd = new SqlCommand(@"
				INSERT INTO [Jobs].[Responsibilities]
					(UId, Value, JobId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RecordStatus)
				VALUES
					(@uid, @value, @jobId, @createdAt, @createdBy, @updatedAt, @updatedBy, 'Active')", conn);
			cmd.Parameters.AddWithValue("@uid", resp.Id);
			cmd.Parameters.AddWithValue("@value", resp.Value);
			cmd.Parameters.AddWithValue("@jobId", newJobId);
			cmd.Parameters.AddWithValue("@createdAt", j.CreatedAt);
			cmd.Parameters.AddWithValue("@createdBy", Truncate(j.CreatedBy, 50));
			cmd.Parameters.AddWithValue("@updatedAt", j.UpdatedAt);
			cmd.Parameters.AddWithValue("@updatedBy", Truncate(j.UpdatedBy, 50));
			cmd.ExecuteNonQuery();
		}

		// Insert qualifications
		foreach (var qual in j.Qualifications)
		{
			using var cmd = new SqlCommand(@"
				INSERT INTO [Jobs].[Qualifications]
					(UId, Value, JobId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RecordStatus)
				VALUES
					(@uid, @value, @jobId, @createdAt, @createdBy, @updatedAt, @updatedBy, 'Active')", conn);
			cmd.Parameters.AddWithValue("@uid", qual.Id);
			cmd.Parameters.AddWithValue("@value", qual.Value);
			cmd.Parameters.AddWithValue("@jobId", newJobId);
			cmd.Parameters.AddWithValue("@createdAt", j.CreatedAt);
			cmd.Parameters.AddWithValue("@createdBy", Truncate(j.CreatedBy, 50));
			cmd.Parameters.AddWithValue("@updatedAt", j.UpdatedAt);
			cmd.Parameters.AddWithValue("@updatedBy", Truncate(j.UpdatedBy, 50));
			cmd.ExecuteNonQuery();
		}

		count++;
		$"Inserted job '{j.Title}' into job-api → Id={newJobId} ({j.Responsibilities.Count} resp, {j.Qualifications.Count} qual)".Dump();
	}
	return count;
}

// ── user-api: [Users].[Companies] ──
// KeycloakGroupId follows the pattern from provisioning: the Keycloak group ID
// Since we don't have the actual Keycloak group IDs here, we use the company Guid as placeholder.
// You may need to update these after running if Keycloak groups already exist.
int UpsertUserApiCompanies(List<MonolithCompany> companies)
{
	int count = 0;
	using var conn = new SqlConnection(MicroConn);
	conn.Open();

	foreach (var c in companies)
	{
		// Check if already exists by UId
		using (var chk = new SqlCommand(
			"SELECT Id FROM [Users].[Companies] WHERE UId = @uid", conn))
		{
			chk.Parameters.AddWithValue("@uid", c.Id);
			if (chk.ExecuteScalar() != null)
			{
				$"Company '{c.Name}' already exists in user-api, skipping".Dump();
				continue;
			}
		}

		// Also check by KeycloakGroupId to avoid unique constraint violation
		string keycloakGroupId = c.Id.ToString();
		using (var chk = new SqlCommand(
			"SELECT Id FROM [Users].[Companies] WHERE KeycloakGroupId = @kgid", conn))
		{
			chk.Parameters.AddWithValue("@kgid", keycloakGroupId);
			if (chk.ExecuteScalar() != null)
			{
				$"Company '{c.Name}' already exists in user-api by KeycloakGroupId, skipping".Dump();
				continue;
			}
		}

		using var cmd = new SqlCommand(@"
			INSERT INTO [Users].[Companies]
				(UId, Name, KeycloakGroupId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RecordStatus)
			VALUES
				(@uid, @name, @keycloakGroupId, @createdAt, @createdBy, @updatedAt, @updatedBy, 'Active')", conn);

		cmd.Parameters.AddWithValue("@uid", c.Id);
		cmd.Parameters.AddWithValue("@name", Truncate(c.Name, 50));
		cmd.Parameters.AddWithValue("@keycloakGroupId", keycloakGroupId);
		cmd.Parameters.AddWithValue("@createdAt", c.CreatedAt);
		cmd.Parameters.AddWithValue("@createdBy", Truncate(c.CreatedBy, 50));
		cmd.Parameters.AddWithValue("@updatedAt", c.UpdatedAt);
		cmd.Parameters.AddWithValue("@updatedBy", Truncate(c.UpdatedBy, 50));

		cmd.ExecuteNonQuery();
		count++;
		$"Inserted company '{c.Name}' into user-api (KeycloakGroupId={keycloakGroupId})".Dump();
	}
	return count;
}

// ── Helpers ──────────────────────────────────────────────────────────────────

string Truncate(string value, int maxLength) =>
	value.Length <= maxLength ? value : value[..maxLength];
