using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bzn.Cloudios.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRealmSlugUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Populate slugs for existing realms with empty slugs
            // Use a simple slug generation: lowercase, replace spaces and special chars with dashes
            migrationBuilder.Sql(@"
                UPDATE Realms 
                SET Slug = lower(
                    replace(
                        replace(
                            replace(
                                replace(
                                    replace(Name, ' ', '-'),
                                    '.', '-'
                                ),
                                '_', '-'
                            ),
                            '/', '-'
                        ),
                        '--', '-'
                    )
                )
                WHERE Slug = '' OR Slug IS NULL
            ");

            // Remove any remaining consecutive dashes and trim
            migrationBuilder.Sql(@"
                UPDATE Realms 
                SET Slug = trim(replace(replace(Slug, '--', '-'), '--', '-'), '-')
                WHERE Slug LIKE '%--%' OR Slug LIKE '-%' OR Slug LIKE '%-'
            ");

            // Generate unique slugs for any collisions by appending GUID
            migrationBuilder.Sql(@"
                UPDATE Realms 
                SET Slug = Slug || '-' || lower(hex(substr(Id, 1, 8)))
                WHERE Id IN (
                    SELECT r1.Id 
                    FROM Realms r1
                    WHERE EXISTS (
                        SELECT 1 FROM Realms r2 
                        WHERE r2.Slug = r1.Slug AND r2.Id != r1.Id
                    )
                )
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Realms_Slug",
                table: "Realms",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Realms_Slug",
                table: "Realms");
        }
    }
}
