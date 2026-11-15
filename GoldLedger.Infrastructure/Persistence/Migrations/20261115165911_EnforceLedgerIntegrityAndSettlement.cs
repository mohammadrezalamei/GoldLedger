using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoldLedger.Infrastructure.Persistence.Migrations
{
    public partial class EnforceLedgerIntegrityAndSettlement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Wallets_Balance",
                table: "Wallets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Wallets_LockedBalance",
                table: "Wallets");

            migrationBuilder.CreateTable(
                name: "DomainEventRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainEventRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentSettlements",
                columns: table => new
                {
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderType = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SettledAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSettlements", x => x.OrderId);
                    table.CheckConstraint("CK_PaymentSettlements_Amount", "CAST(Amount AS NUMERIC) >= 0");
                    table.CheckConstraint("CK_PaymentSettlements_OrderType", "OrderType IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_PaymentSettlements_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Wallets_Balance",
                table: "Wallets",
                sql: "CAST(Balance AS NUMERIC) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Wallets_LockedBalance",
                table: "Wallets",
                sql: "CAST(LockedBalance AS NUMERIC) >= 0 AND CAST(LockedBalance AS NUMERIC) <= CAST(Balance AS NUMERIC)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderTags_Value",
                table: "OrderTags",
                sql: "length(trim(Value)) BETWEEN 1 AND 50");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_CreatedAt",
                table: "Orders",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_FeePercent",
                table: "Orders",
                sql: "CAST(FeePercent AS NUMERIC) BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Status",
                table: "Orders",
                sql: "Status IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_TotalAmount",
                table: "Orders",
                sql: "CAST(TotalAmount AS NUMERIC) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Type",
                table: "Orders",
                sql: "Type IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_UnitPrice",
                table: "Orders",
                sql: "CAST(UnitPrice AS NUMERIC) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_UserNote",
                table: "Orders",
                sql: "UserNote IS NULL OR length(UserNote) <= 1000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Weight",
                table: "Orders",
                sql: "CAST(Weight AS NUMERIC) > 0");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEventRecords_OrderId",
                table: "DomainEventRecords",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSettlements_UserId",
                table: "PaymentSettlements",
                column: "UserId");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DomainEventRecords");

            migrationBuilder.DropTable(
                name: "PaymentSettlements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Wallets_Balance",
                table: "Wallets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Wallets_LockedBalance",
                table: "Wallets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderTags_Value",
                table: "OrderTags");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId_CreatedAt",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_FeePercent",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Status",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_TotalAmount",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Type",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_UnitPrice",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_UserNote",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Weight",
                table: "Orders");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Wallets_Balance",
                table: "Wallets",
                sql: "Balance >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Wallets_LockedBalance",
                table: "Wallets",
                sql: "LockedBalance >= 0 AND LockedBalance <= Balance");
        }
    }
}
