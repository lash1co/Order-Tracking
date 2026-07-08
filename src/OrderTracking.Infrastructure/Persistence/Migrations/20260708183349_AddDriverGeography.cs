using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace OrderTracking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverGeography : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Drivers",
                type: "geography",
                nullable: false,
                defaultValueSql: "geography::Point(0, 0, 4326)");

            migrationBuilder.Sql(
                "CREATE SPATIAL INDEX [SIX_Drivers_Location] ON [Drivers]([Location]) " +
                "USING GEOGRAPHY_AUTO_GRID WITH (CELLS_PER_OBJECT = 16)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX [SIX_Drivers_Location] ON [Drivers]");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Drivers");
        }
    }
}
