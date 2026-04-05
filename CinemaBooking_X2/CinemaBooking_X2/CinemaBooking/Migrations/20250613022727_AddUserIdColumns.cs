using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only add user_id columns - skip Identity tables creation for now
            migrationBuilder.AddColumn<string>(
                name: "user_id",
                table: "otp_info",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_id",
                table: "lich_su_giao_dich",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_id",
                table: "dat_ve",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_id",
                table: "danh_gia",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            // Create indexes for the new user_id columns
            migrationBuilder.CreateIndex(
                name: "IX_otp_info_user_id",
                table: "otp_info",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_lich_su_giao_dich_user_id",
                table: "lich_su_giao_dich",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dat_ve_user_id",
                table: "dat_ve",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_danh_gia_user_id",
                table: "danh_gia",
                column: "user_id");

            // Add foreign keys to Identity tables (assuming they exist)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sysobjects WHERE name='AspNetUsers' AND xtype='U')
                BEGIN
                    ALTER TABLE [danh_gia] ADD CONSTRAINT [FK_danh_gia_AspNetUsers_user_id]
                    FOREIGN KEY ([user_id]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL;

                    ALTER TABLE [dat_ve] ADD CONSTRAINT [FK_dat_ve_AspNetUsers_user_id]
                    FOREIGN KEY ([user_id]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL;

                    ALTER TABLE [lich_su_giao_dich] ADD CONSTRAINT [FK_lich_su_giao_dich_AspNetUsers_user_id]
                    FOREIGN KEY ([user_id]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL;

                    ALTER TABLE [otp_info] ADD CONSTRAINT [FK_otp_info_AspNetUsers_user_id]
                    FOREIGN KEY ([user_id]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys if they exist
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_danh_gia_AspNetUsers_user_id')
                    ALTER TABLE [danh_gia] DROP CONSTRAINT [FK_danh_gia_AspNetUsers_user_id];

                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dat_ve_AspNetUsers_user_id')
                    ALTER TABLE [dat_ve] DROP CONSTRAINT [FK_dat_ve_AspNetUsers_user_id];

                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_lich_su_giao_dich_AspNetUsers_user_id')
                    ALTER TABLE [lich_su_giao_dich] DROP CONSTRAINT [FK_lich_su_giao_dich_AspNetUsers_user_id];

                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_otp_info_AspNetUsers_user_id')
                    ALTER TABLE [otp_info] DROP CONSTRAINT [FK_otp_info_AspNetUsers_user_id];
            ");

            migrationBuilder.DropIndex(
                name: "IX_otp_info_user_id",
                table: "otp_info");

            migrationBuilder.DropIndex(
                name: "IX_lich_su_giao_dich_user_id",
                table: "lich_su_giao_dich");

            migrationBuilder.DropIndex(
                name: "IX_dat_ve_user_id",
                table: "dat_ve");

            migrationBuilder.DropIndex(
                name: "IX_danh_gia_user_id",
                table: "danh_gia");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "otp_info");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "lich_su_giao_dich");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "dat_ve");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "danh_gia");
        }
    }
}
