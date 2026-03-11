<Query Kind="Program">
  <NuGetReference>Microsoft.Data.SqlClient</NuGetReference>
  <Namespace>Microsoft.Data.SqlClient</Namespace>
</Query>

// =============================================================================
// Copy Companies & Jobs from Microservices → Monolith
// =============================================================================
// Reads from company-api and job-api microservice databases,
// inserts into the monolith database preserving GUIDs (UId → Id).
// Monolith InternalId is auto-generated via SQL sequences.
// =============================================================================

// ── Connection Strings ── plug these in ──────────────────────────────────────
const string CompanyApiConn = "Server=sqlserver.eelkhair.net;Database=Job-board;user id=sa;password=Pass321$;TrustServerCertificate=True;MultipleActiveResultSets=True";
const string JobApiConn = "Server=sqlserver.eelkhair.net;Database=Job-board;user id=sa;password=Pass321$;TrustServerCertificate=True;MultipleActiveResultSets=True";
const string MonolithConn = "Server=sqlserver.eelkhair.net;Database=Job-board-Monolith;user id=sa;password=Pass321$;TrustServerCertificate=True;MultipleActiveResultSets=True";

void Main()
{
	// 1. Read industries from company-api
	var microIndustries = ReadMicroIndustries();
	$"Found {microIndustries.Count} industries in company-api".Dump("Step 1");

	// 2. Read companies from company-api
	var microCompanies = ReadMicroCompanies();
	$"Found {microCompanies.Count} companies in company-api".Dump("Step 2");

	// 3. Read jobs (with responsibilities & qualifications) from job-api
	var microJobs = ReadMicroJobs();
	$"Found {microJobs.Count} jobs in job-api".Dump("Step 3");

	// 4. Resolve industry mapping (match by name to existing monolith industries)
	var industryMap = ResolveIndustryMap(microIndustries);
	industryMap.Dump("Step 4 — Industry Name → Monolith InternalId");

	// 5. Insert companies into monolith, build UId → new InternalId map
	var companyMap = InsertCompanies(microCompanies, industryMap);
	companyMap.Dump("Step 5 — Company UId → Monolith InternalId");

	// 6. Build job-api CompanyId → monolith InternalId map via UId
	var jobApiCompanyUIds = ReadJobApiCompanyUIds();
	var jobCompanyIdToMonolithId = new Dictionary<int, int>();
	foreach (var jc in jobApiCompanyUIds)
	{
		if (companyMap.TryGetValue(jc.UId, out int monolithId))
			jobCompanyIdToMonolithId[jc.Id] = monolithId;
	}

	// 7. Insert jobs, responsibilities, qualifications
	var jobsInserted = InsertJobs(microJobs, jobCompanyIdToMonolithId);
	$"Inserted {jobsInserted} jobs with responsibilities & qualifications".Dump("Step 7 — Done");
}

// ── Data Models ──────────────────────────────────────────────────────────────

record MicroIndustry(int Id, Guid UId, string Name);

record MicroCompany(
	int Id, Guid UId, string Name, string Email, string Status,
	string? Description, string? Website, string? Phone, string? About,
	string? EEO, DateTime? Founded, string? Size, string? Logo,
	int IndustryId, string IndustryName,
	DateTime CreatedAt, string CreatedBy, DateTime? UpdatedAt, string UpdatedBy
);

record MicroJob(
	int Id, Guid UId, string Title, string Location, string AboutRole,
	string? SalaryRange, string JobType, int CompanyId,
	DateTime CreatedAt, string CreatedBy, DateTime? UpdatedAt, string UpdatedBy,
	List<MicroChild> Responsibilities, List<MicroChild> Qualifications
);

record MicroChild(Guid UId, string Value);

record IdPair(int Id, Guid UId);

// ── Readers ──────────────────────────────────────────────────────────────────

List<MicroIndustry> ReadMicroIndustries()
{
	var list = new List<MicroIndustry>();
	using var conn = new SqlConnection(CompanyApiConn);
	conn.Open();
	using var cmd = new SqlCommand(
		"SELECT Id, UId, Name FROM [Company].[Industries] WHERE RecordStatus = 'Active'", conn);
	using var r = cmd.ExecuteReader();
	while (r.Read())
		list.Add(new(r.GetInt32(0), r.GetGuid(1), r.GetString(2)));
	return list;
}

List<MicroCompany> ReadMicroCompanies()
{
	var list = new List<MicroCompany>();
	using var conn = new SqlConnection(CompanyApiConn);
	conn.Open();
	using var cmd = new SqlCommand(@"
		SELECT c.Id, c.UId, c.Name, c.Email, c.Status,
		       c.Description, c.Website, c.Phone, c.About, c.EEO,
		       c.Founded, c.Size, c.Logo, c.IndustryId,
		       i.Name AS IndustryName,
		       c.CreatedAt, c.CreatedBy, c.UpdatedAt, c.UpdatedBy
		FROM [Company].[Companies] c
		JOIN [Company].[Industries] i ON c.IndustryId = i.Id
		WHERE c.RecordStatus = 'Active'", conn);
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
			r.GetInt32(13), r.GetString(14),
			r.GetDateTime(15), r.GetString(16),
			r.IsDBNull(17) ? null : r.GetDateTime(17), r.GetString(18)
		));
	}
	return list;
}

List<MicroJob> ReadMicroJobs()
{
	var jobs = new List<MicroJob>();
	using var conn = new SqlConnection(JobApiConn);
	conn.Open();

	// Read jobs
	using (var cmd = new SqlCommand(@"
		SELECT Id, UId, Title, Location, AboutRole, SalaryRange, JobType,
		       CompanyId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
		FROM [Jobs].[Jobs]
		WHERE RecordStatus = 'Active'", conn))
	using (var r = cmd.ExecuteReader())
	{
		while (r.Read())
		{
			jobs.Add(new(
				r.GetInt32(0), r.GetGuid(1), r.GetString(2), r.GetString(3),
				r.GetString(4), r.IsDBNull(5) ? null : r.GetString(5),
				r.GetString(6), r.GetInt32(7),
				r.GetDateTime(8), r.GetString(9),
				r.IsDBNull(10) ? null : r.GetDateTime(10), r.GetString(11),
				new List<MicroChild>(), new List<MicroChild>()
			));
		}
	}

	// Read responsibilities
	var respByJob = ReadChildren(conn, "[Jobs].[Responsibilities]");
	var qualByJob = ReadChildren(conn, "[Jobs].[Qualifications]");

	foreach (var job in jobs)
	{
		if (respByJob.TryGetValue(job.Id, out var resps))
			job.Responsibilities.AddRange(resps);
		if (qualByJob.TryGetValue(job.Id, out var quals))
			job.Qualifications.AddRange(quals);
	}
	return jobs;
}

Dictionary<int, List<MicroChild>> ReadChildren(SqlConnection conn, string table)
{
	var dict = new Dictionary<int, List<MicroChild>>();
	using var cmd = new SqlCommand(
		$"SELECT JobId, UId, Value FROM {table} WHERE RecordStatus = 'Active'", conn);
	using var r = cmd.ExecuteReader();
	while (r.Read())
	{
		int jobId = r.GetInt32(0);
		if (!dict.ContainsKey(jobId)) dict[jobId] = new();
		dict[jobId].Add(new(r.GetGuid(1), r.GetString(2)));
	}
	return dict;
}

List<IdPair> ReadJobApiCompanyUIds()
{
	var list = new List<IdPair>();
	using var conn = new SqlConnection(JobApiConn);
	conn.Open();
	using var cmd = new SqlCommand(
		"SELECT Id, UId FROM [Jobs].[Companies] WHERE RecordStatus = 'Active'", conn);
	using var r = cmd.ExecuteReader();
	while (r.Read())
		list.Add(new(r.GetInt32(0), r.GetGuid(1)));
	return list;
}

// ── Writers ──────────────────────────────────────────────────────────────────

Dictionary<string, int> ResolveIndustryMap(List<MicroIndustry> microIndustries)
{
	// Map industry Name → monolith InternalId
	var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	using var conn = new SqlConnection(MonolithConn);
	conn.Open();
	using var cmd = new SqlCommand(
		"SELECT InternalId, Name FROM [Company].[Industries]", conn);
	using var r = cmd.ExecuteReader();
	while (r.Read())
		map[r.GetString(1)] = r.GetInt32(0);

	// Check all micro industries exist in monolith
	foreach (var mi in microIndustries)
	{
		if (!map.ContainsKey(mi.Name))
			$"WARNING: Industry '{mi.Name}' not found in monolith — companies with this industry will fail!".Dump();
	}
	return map;
}

Dictionary<Guid, int> InsertCompanies(List<MicroCompany> companies, Dictionary<string, int> industryMap)
{
	// Returns UId → new monolith InternalId
	var map = new Dictionary<Guid, int>();
	using var conn = new SqlConnection(MonolithConn);
	conn.Open();

	foreach (var c in companies)
	{
		if (!industryMap.TryGetValue(c.IndustryName, out int monolithIndustryId))
		{
			$"SKIPPED company '{c.Name}' — industry '{c.IndustryName}' not in monolith".Dump();
			continue;
		}

		// Check if company already exists by Id (Guid)
		using (var chk = new SqlCommand(
			"SELECT InternalId FROM [Company].[Companies] WHERE Id = @uid", conn))
		{
			chk.Parameters.AddWithValue("@uid", c.UId);
			var existing = chk.ExecuteScalar();
			if (existing != null)
			{
				map[c.UId] = (int)existing;
				$"Company '{c.Name}' already exists (InternalId={existing}), skipping insert".Dump();
				continue;
			}
		}

		// Get next sequence value for InternalId
		int newId;
		using (var seq = new SqlCommand(
			"SELECT NEXT VALUE FOR Companies_Sequence", conn))
		{
			newId = Convert.ToInt32(seq.ExecuteScalar());
		}

		using var cmd = new SqlCommand(@"
			INSERT INTO [Company].[Companies]
				(InternalId, Id, Name, Email, Status, Description, Website, Phone,
				 About, EEO, Founded, Size, Logo, IndustryId,
				 CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
			VALUES
				(@internalId, @id, @name, @email, @status, @desc, @website, @phone,
				 @about, @eeo, @founded, @size, @logo, @industryId,
				 @createdAt, @createdBy, @updatedAt, @updatedBy)", conn);

		cmd.Parameters.AddWithValue("@internalId", newId);
		cmd.Parameters.AddWithValue("@id", c.UId);
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
		cmd.Parameters.AddWithValue("@industryId", monolithIndustryId);
		cmd.Parameters.AddWithValue("@createdAt", c.CreatedAt);
		cmd.Parameters.AddWithValue("@createdBy", c.CreatedBy);
		cmd.Parameters.AddWithValue("@updatedAt", (object?)c.UpdatedAt ?? DateTime.UtcNow);
		cmd.Parameters.AddWithValue("@updatedBy", c.UpdatedBy);

		cmd.ExecuteNonQuery();
		map[c.UId] = newId;
		$"Inserted company '{c.Name}' → InternalId={newId}".Dump();
	}
	return map;
}

int InsertJobs(List<MicroJob> jobs, Dictionary<int, int> jobApiCompanyIdToMonolithId)
{
	int count = 0;
	using var conn = new SqlConnection(MonolithConn);
	conn.Open();

	foreach (var j in jobs)
	{
		if (!jobApiCompanyIdToMonolithId.TryGetValue(j.CompanyId, out int monolithCompanyId))
		{
			$"SKIPPED job '{j.Title}' — CompanyId {j.CompanyId} has no monolith mapping".Dump();
			continue;
		}

		// Check if job already exists by Id (Guid)
		using (var chk = new SqlCommand(
			"SELECT InternalId FROM [Job].[Jobs] WHERE Id = @uid", conn))
		{
			chk.Parameters.AddWithValue("@uid", j.UId);
			if (chk.ExecuteScalar() != null)
			{
				$"Job '{j.Title}' already exists, skipping".Dump();
				continue;
			}
		}

		// Convert JobType string → int enum
		int jobTypeInt = j.JobType switch
		{
			"FullTime" => 0,
			"PartTime" => 1,
			"Contract" => 2,
			"Internship" => 3,
			_ => throw new Exception($"Unknown JobType: {j.JobType}")
		};

		// Get next sequence value
		int newJobId;
		using (var seq = new SqlCommand(
			"SELECT NEXT VALUE FOR Jobs_Sequence", conn))
		{
			newJobId = Convert.ToInt32(seq.ExecuteScalar());
		}

		using (var cmd = new SqlCommand(@"
			INSERT INTO [Job].[Jobs]
				(InternalId, Id, Title, Location, AboutRole, SalaryRange, JobType,
				 CompanyId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
			VALUES
				(@internalId, @id, @title, @location, @aboutRole, @salaryRange, @jobType,
				 @companyId, @createdAt, @createdBy, @updatedAt, @updatedBy)", conn))
		{
			cmd.Parameters.AddWithValue("@internalId", newJobId);
			cmd.Parameters.AddWithValue("@id", j.UId);
			cmd.Parameters.AddWithValue("@title", j.Title);
			cmd.Parameters.AddWithValue("@location", j.Location);
			cmd.Parameters.AddWithValue("@aboutRole", j.AboutRole);
			cmd.Parameters.AddWithValue("@salaryRange", (object?)j.SalaryRange ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@jobType", jobTypeInt);
			cmd.Parameters.AddWithValue("@companyId", monolithCompanyId);
			cmd.Parameters.AddWithValue("@createdAt", j.CreatedAt);
			cmd.Parameters.AddWithValue("@createdBy", j.CreatedBy);
			cmd.Parameters.AddWithValue("@updatedAt", (object?)j.UpdatedAt ?? DateTime.UtcNow);
			cmd.Parameters.AddWithValue("@updatedBy", j.UpdatedBy);
			cmd.ExecuteNonQuery();
		}

		// Insert responsibilities
		foreach (var resp in j.Responsibilities)
		{
			int respId;
			using (var seq = new SqlCommand(
				"SELECT NEXT VALUE FOR Responsibilities_Sequence", conn))
			{
				respId = Convert.ToInt32(seq.ExecuteScalar());
			}

			using var cmd = new SqlCommand(@"
				INSERT INTO [Job].[Responsibilities]
					(InternalId, Id, Value, JobId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
				VALUES
					(@internalId, @id, @value, @jobId, @createdAt, @createdBy, @updatedAt, @updatedBy)", conn);
			cmd.Parameters.AddWithValue("@internalId", respId);
			cmd.Parameters.AddWithValue("@id", resp.UId);
			cmd.Parameters.AddWithValue("@value", resp.Value);
			cmd.Parameters.AddWithValue("@jobId", newJobId);
			cmd.Parameters.AddWithValue("@createdAt", j.CreatedAt);
			cmd.Parameters.AddWithValue("@createdBy", j.CreatedBy);
			cmd.Parameters.AddWithValue("@updatedAt", (object?)j.UpdatedAt ?? DateTime.UtcNow);
			cmd.Parameters.AddWithValue("@updatedBy", j.UpdatedBy);
			cmd.ExecuteNonQuery();
		}

		// Insert qualifications
		foreach (var qual in j.Qualifications)
		{
			int qualId;
			using (var seq = new SqlCommand(
				"SELECT NEXT VALUE FOR Qualifications_Sequence", conn))
			{
				qualId = Convert.ToInt32(seq.ExecuteScalar());
			}

			using var cmd = new SqlCommand(@"
				INSERT INTO [Job].[Qualifications]
					(InternalId, Id, Value, JobId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
				VALUES
					(@internalId, @id, @value, @jobId, @createdAt, @createdBy, @updatedAt, @updatedBy)", conn);
			cmd.Parameters.AddWithValue("@internalId", qualId);
			cmd.Parameters.AddWithValue("@id", qual.UId);
			cmd.Parameters.AddWithValue("@value", qual.Value);
			cmd.Parameters.AddWithValue("@jobId", newJobId);
			cmd.Parameters.AddWithValue("@createdAt", j.CreatedAt);
			cmd.Parameters.AddWithValue("@createdBy", j.CreatedBy);
			cmd.Parameters.AddWithValue("@updatedAt", (object?)j.UpdatedAt ?? DateTime.UtcNow);
			cmd.Parameters.AddWithValue("@updatedBy", j.UpdatedBy);
			cmd.ExecuteNonQuery();
		}

		count++;
		$"Inserted job '{j.Title}' → InternalId={newJobId} ({j.Responsibilities.Count} resp, {j.Qualifications.Count} qual)".Dump();
	}
	return count;
}
