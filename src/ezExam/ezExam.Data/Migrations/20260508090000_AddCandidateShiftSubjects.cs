using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ezExam.Data.Migrations
{
    public partial class AddCandidateShiftSubjects : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Shift1SubjectName",
                table: "Candidates",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Shift2SubjectName",
                table: "Candidates",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Shift1SubjectName",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "Shift2SubjectName",
                table: "Candidates");
        }
    }
}
