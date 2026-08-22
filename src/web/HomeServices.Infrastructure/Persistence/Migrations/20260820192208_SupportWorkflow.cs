using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SupportWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpertPayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayoutNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    WorkCompletionReportId = table.Column<int>(type: "int", nullable: false),
                    ExpertId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ServiceTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PaidBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpertPayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpertPayouts_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentVerificationReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SenderFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BankRefNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SupportNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentVerificationReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentVerificationReports_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentVerificationReports_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WorkCompletionReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpertId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpertConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    ExpertConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpertNote = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CustomerConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    CustomerConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CustomerNote = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SupportNote = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCompletionReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkCompletionReports_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkCompletionAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkCompletionReportId = table.Column<int>(type: "int", nullable: false),
                    Uploader = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MediaType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCompletionAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkCompletionAttachments_WorkCompletionReports_WorkCompletionReportId",
                        column: x => x.WorkCompletionReportId,
                        principalTable: "WorkCompletionReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpertPayouts_ExpertId",
                table: "ExpertPayouts",
                column: "ExpertId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertPayouts_OrderId",
                table: "ExpertPayouts",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpertPayouts_PaidAt",
                table: "ExpertPayouts",
                column: "PaidAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertPayouts_PayoutNumber",
                table: "ExpertPayouts",
                column: "PayoutNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerificationReports_CreatedAt",
                table: "PaymentVerificationReports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerificationReports_CustomerId",
                table: "PaymentVerificationReports",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerificationReports_OrderId",
                table: "PaymentVerificationReports",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerificationReports_PaymentId",
                table: "PaymentVerificationReports",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerificationReports_Status",
                table: "PaymentVerificationReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCompletionAttachments_WorkCompletionReportId",
                table: "WorkCompletionAttachments",
                column: "WorkCompletionReportId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCompletionReports_CreatedAt",
                table: "WorkCompletionReports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCompletionReports_CustomerId",
                table: "WorkCompletionReports",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCompletionReports_ExpertId",
                table: "WorkCompletionReports",
                column: "ExpertId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCompletionReports_OrderId",
                table: "WorkCompletionReports",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCompletionReports_Status",
                table: "WorkCompletionReports",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpertPayouts");

            migrationBuilder.DropTable(
                name: "PaymentVerificationReports");

            migrationBuilder.DropTable(
                name: "WorkCompletionAttachments");

            migrationBuilder.DropTable(
                name: "WorkCompletionReports");
        }
    }
}
