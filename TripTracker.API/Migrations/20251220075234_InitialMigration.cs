using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TripTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TripStops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripStops_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Trips",
                columns: new[] { "Id", "Description", "EndDate", "ImageUrl", "Name", "StartDate" },
                values: new object[,]
                {
                    { 1, "Avontuurlijke roadtrip door West-Europa", new DateTime(2024, 7, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Roadtrip Europa 2024", new DateTime(2024, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, "Romantisch weekendje Parijs", new DateTime(2024, 8, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Weekend Parijs", new DateTime(2024, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "TripStops",
                columns: new[] { "Id", "Address", "Country", "DateTime", "Description", "Latitude", "Longitude", "PhotoUrl", "Title", "TripId" },
                values: new object[,]
                {
                    { 1, "Champ de Mars, Paris, France", "France", new DateTime(2024, 7, 2, 14, 30, 0, 0, DateTimeKind.Unspecified), "De iconische Eiffeltoren in Parijs", 48.858400000000003, 2.2945000000000002, null, "Eiffeltoren", 1 },
                    { 2, "Pariser Platz, Berlin, Germany", "Germany", new DateTime(2024, 7, 5, 10, 0, 0, 0, DateTimeKind.Unspecified), "Historisch monument in Berlijn", 52.516300000000001, 13.377700000000001, null, "Brandenburger Tor", 1 },
                    { 3, "Rue de Rivoli, Paris, France", "France", new DateTime(2024, 8, 16, 9, 0, 0, 0, DateTimeKind.Unspecified), "Wereldberoemd kunstmuseum met de Mona Lisa", 48.860599999999998, 2.3376000000000001, null, "Louvre Museum", 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TripStops_TripId",
                table: "TripStops",
                column: "TripId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripStops");

            migrationBuilder.DropTable(
                name: "Trips");
        }
    }
}
