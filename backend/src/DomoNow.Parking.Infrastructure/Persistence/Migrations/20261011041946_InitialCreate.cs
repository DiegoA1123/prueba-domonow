using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomoNow.Parking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "operators",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operators", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parking_spots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parking_spots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parking_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parking_spot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    license_plate = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    visitor_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    destination_unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    checked_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    checked_out_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parking_assignments", x => x.id);
                    table.CheckConstraint("ck_parking_assignments_checkout_after_checkin", "checked_out_at IS NULL OR checked_out_at >= checked_in_at");
                    table.ForeignKey(
                        name: "fk_parking_assignments_parking_spots_parking_spot_id",
                        column: x => x.parking_spot_id,
                        principalTable: "parking_spots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_operators_username",
                table: "operators",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_parking_assignments_active_plate",
                table: "parking_assignments",
                column: "license_plate",
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ux_parking_assignments_active_spot",
                table: "parking_assignments",
                column: "parking_spot_id",
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_parking_spots_code",
                table: "parking_spots",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operators");

            migrationBuilder.DropTable(
                name: "parking_assignments");

            migrationBuilder.DropTable(
                name: "parking_spots");
        }
    }
}
