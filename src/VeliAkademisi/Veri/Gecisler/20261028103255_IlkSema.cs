using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeliAkademisi.Veri.Gecisler
{
    /// <inheritdoc />
    public partial class IlkSema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Veliler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Sinif = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Kod = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SonGiris = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veliler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cevaplar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VeliId = table.Column<int>(type: "INTEGER", nullable: false),
                    Hikaye = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Adim = table.Column<int>(type: "INTEGER", nullable: false),
                    Secim = table.Column<int>(type: "INTEGER", nullable: false),
                    Zaman = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cevaplar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cevaplar_Veliler_VeliId",
                        column: x => x.VeliId,
                        principalTable: "Veliler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ilerlemeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VeliId = table.Column<int>(type: "INTEGER", nullable: false),
                    Hikaye = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    EnIleriAdim = table.Column<int>(type: "INTEGER", nullable: false),
                    SonAdim = table.Column<int>(type: "INTEGER", nullable: false),
                    Tamamlandi = table.Column<bool>(type: "INTEGER", nullable: false),
                    IlkGiris = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SonGuncelleme = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ilerlemeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ilerlemeler_Veliler_VeliId",
                        column: x => x.VeliId,
                        principalTable: "Veliler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cevaplar_VeliId_Hikaye_Adim",
                table: "Cevaplar",
                columns: new[] { "VeliId", "Hikaye", "Adim" });

            migrationBuilder.CreateIndex(
                name: "IX_Ilerlemeler_VeliId_Hikaye",
                table: "Ilerlemeler",
                columns: new[] { "VeliId", "Hikaye" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Veliler_Kod",
                table: "Veliler",
                column: "Kod",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cevaplar");

            migrationBuilder.DropTable(
                name: "Ilerlemeler");

            migrationBuilder.DropTable(
                name: "Veliler");
        }
    }
}
