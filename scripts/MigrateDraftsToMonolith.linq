<Query Kind="Program">
  <NuGetReference>Microsoft.Data.SqlClient</NuGetReference>
  <NuGetReference>Npgsql</NuGetReference>
  <Namespace>Microsoft.Data.SqlClient</Namespace>
  <Namespace>Npgsql</Namespace>
</Query>

// =============================================================================
// Migrate Drafts: AI Service (PostgreSQL) → Monolith (SQL Server)
// =============================================================================
// Reads drafts from AI service's PostgreSQL `drafts` table and inserts them
// into the monolith's [Draft].[Drafts] table.
//
// Prerequisites:
//   1. Monolith AddDraftsTable migration has been applied
//   2. AI service PostgreSQL is accessible
//
// Idempotent: skips drafts where Id already exists in the target table.
// =============================================================================

// ── Connection Strings ──────────────────────────────────────────────────────
const string PostgresConn = "Host=postgres.eelkhair.net;Database=ai-service;Username=postgres;Password=Pass321$";
const string MonolithConn = "Server=sqlserver.eelkhair.net;Database=Job-board-Monolith;user id=sa;password=Pass321$;TrustServerCertificate=True;MultipleActiveResultSets=True";

void Main()
{
	// 1. Read all drafts from AI service PostgreSQL
	var drafts = ReadPostgresDrafts();
	$"Found {drafts.Count} drafts in AI service".Dump("Step 1 — Read Source");

	if (drafts.Count == 0)
	{
		"No drafts to migrate.".Dump("Done");
		return;
	}

	// 2. Insert into monolith SQL Server
	var (inserted, skipped) = InsertDraftsToMonolith(drafts);
	$"Inserted: {inserted}, Skipped (already exists): {skipped}".Dump("Step 2 — Done");
}

// ── Data Model ──────────────────────────────────────────────────────────────

record PgDraft(
	Guid Id,
	Guid CompanyId,
	string Type,
	string Status,
	string ContentJson,
	DateTime CreatedAt,
	DateTime UpdatedAt
);

// ── Reader: PostgreSQL ──────────────────────────────────────────────────────

List<PgDraft> ReadPostgresDrafts()
{
	var list = new List<PgDraft>();
	using var conn = new NpgsqlConnection(PostgresConn);
	conn.Open();

	using var cmd = new NpgsqlCommand(@"
		SELECT ""Id"", ""CompanyId"", ""Type"", ""Status"", ""ContentJson"",
		       ""CreatedAt"", ""UpdatedAt""
		FROM drafts
		ORDER BY ""CreatedAt""", conn);

	using var r = cmd.ExecuteReader();
	while (r.Read())
	{
		list.Add(new PgDraft(
			Id: r.GetGuid(0),
			CompanyId: r.GetGuid(1),
			Type: r.GetString(2),
			Status: r.GetString(3),
			ContentJson: r.GetString(4),
			CreatedAt: r.GetDateTime(5),
			UpdatedAt: r.GetDateTime(6)
		));
	}
	return list;
}

// ── Writer: SQL Server ──────────────────────────────────────────────────────

(int inserted, int skipped) InsertDraftsToMonolith(List<PgDraft> drafts)
{
	int inserted = 0, skipped = 0;
	using var conn = new SqlConnection(MonolithConn);
	conn.Open();

	foreach (var d in drafts)
	{
		// Idempotency check — skip if Id already exists
		using (var chk = new SqlCommand(
			"SELECT 1 FROM [Draft].[Drafts] WHERE Id = @id", conn))
		{
			chk.Parameters.AddWithValue("@id", d.Id);
			if (chk.ExecuteScalar() != null)
			{
				$"Draft {d.Id} already exists, skipping".Dump();
				skipped++;
				continue;
			}
		}

		// Get next sequence value for InternalId
		int newId;
		using (var seq = new SqlCommand(
			"SELECT NEXT VALUE FOR Drafts_Sequence", conn))
		{
			newId = Convert.ToInt32(seq.ExecuteScalar());
		}

		// Map PostgreSQL status values to monolith conventions
		var draftStatus = d.Status switch
		{
			"Generated" or "generated" => "generated",
			"Finalized" or "finalized" => "finalized",
			_ => "draft"
		};

		using var cmd = new SqlCommand(@"
			INSERT INTO [Draft].[Drafts]
				(InternalId, Id, CompanyId, DraftType, DraftStatus, ContentJson,
				 CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
			VALUES
				(@internalId, @id, @companyId, @draftType, @draftStatus, @contentJson,
				 @createdAt, @createdBy, @updatedAt, @updatedBy)", conn);

		cmd.Parameters.AddWithValue("@internalId", newId);
		cmd.Parameters.AddWithValue("@id", d.Id);
		cmd.Parameters.AddWithValue("@companyId", d.CompanyId);
		cmd.Parameters.AddWithValue("@draftType", d.Type);
		cmd.Parameters.AddWithValue("@draftStatus", draftStatus);
		cmd.Parameters.AddWithValue("@contentJson", d.ContentJson);
		cmd.Parameters.AddWithValue("@createdAt", d.CreatedAt);
		cmd.Parameters.AddWithValue("@createdBy", "migration-script");
		cmd.Parameters.AddWithValue("@updatedAt", d.UpdatedAt);
		cmd.Parameters.AddWithValue("@updatedBy", "migration-script");

		cmd.ExecuteNonQuery();
		inserted++;
		$"Inserted draft {d.Id} (Company: {d.CompanyId}, Type: {d.Type}) → InternalId={newId}".Dump();
	}
	return (inserted, skipped);
}
