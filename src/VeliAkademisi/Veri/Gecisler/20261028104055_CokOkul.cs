using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeliAkademisi.Veri.Gecisler
{
    /// <inheritdoc />
    public partial class CokOkul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OkulId",
                table: "Veliler",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Okullar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ad = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Etkin = table.Column<bool>(type: "INTEGER", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Okullar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AdSoyad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    KullaniciAdi = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    SifreOzeti = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Rol = table.Column<int>(type: "INTEGER", nullable: false),
                    OkulId = table.Column<int>(type: "INTEGER", nullable: true),
                    GeciciSifre = table.Column<bool>(type: "INTEGER", nullable: false),
                    Etkin = table.Column<bool>(type: "INTEGER", nullable: false),
                    GuvenlikDamgasi = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SonGiris = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kullanicilar_Okullar_OkulId",
                        column: x => x.OkulId,
                        principalTable: "Okullar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Veliler_OkulId",
                table: "Veliler",
                column: "OkulId");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_KullaniciAdi",
                table: "Kullanicilar",
                column: "KullaniciAdi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_OkulId",
                table: "Kullanicilar",
                column: "OkulId");

            migrationBuilder.CreateIndex(
                name: "IX_Okullar_Ad",
                table: "Okullar",
                column: "Ad",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Veliler_Okullar_OkulId",
                table: "Veliler",
                column: "OkulId",
                principalTable: "Okullar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Veliler_Okullar_OkulId",
                table: "Veliler");

            migrationBuilder.DropTable(
                name: "Kullanicilar");

            migrationBuilder.DropTable(
                name: "Okullar");

            migrationBuilder.DropIndex(
                name: "IX_Veliler_OkulId",
                table: "Veliler");

            migrationBuilder.DropColumn(
                name: "OkulId",
                table: "Veliler");
        }
    }
}
