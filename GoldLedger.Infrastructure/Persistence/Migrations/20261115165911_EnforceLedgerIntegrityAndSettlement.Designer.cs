using System;
using GoldLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace GoldLedger.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(GoldLedgerDbContext))]
    [Migration("20261115165911_EnforceLedgerIntegrityAndSettlement")]
    partial class EnforceLedgerIntegrityAndSettlement
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "10.0.0");

            modelBuilder.Entity("GoldLedger.Domain.Aggregates.GoldOrder.GoldOrder", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("ConcurrencyToken")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("FeePercent")
                        .HasPrecision(9, 4)
                        .HasColumnType("TEXT");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("TotalAmount")
                        .HasPrecision(18, 2)
                        .HasColumnType("TEXT");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("UnitPrice")
                        .HasPrecision(18, 2)
                        .HasColumnType("TEXT");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserNote")
                        .HasMaxLength(1000)
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Weight")
                        .HasPrecision(18, 6)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "CreatedAt");

                    b.ToTable("Orders", null, t =>
                        {
                            t.HasCheckConstraint("CK_Orders_FeePercent", "CAST(FeePercent AS NUMERIC) BETWEEN 0 AND 100");

                            t.HasCheckConstraint("CK_Orders_Status", "Status IN (1, 2, 3)");

                            t.HasCheckConstraint("CK_Orders_TotalAmount", "CAST(TotalAmount AS NUMERIC) >= 0");

                            t.HasCheckConstraint("CK_Orders_Type", "Type IN (1, 2)");

                            t.HasCheckConstraint("CK_Orders_UnitPrice", "CAST(UnitPrice AS NUMERIC) > 0");

                            t.HasCheckConstraint("CK_Orders_UserNote", "UserNote IS NULL OR length(UserNote) <= 1000");

                            t.HasCheckConstraint("CK_Orders_Weight", "CAST(Weight AS NUMERIC) > 0");
                        });
                });

            modelBuilder.Entity("GoldLedger.Domain.Aggregates.GoldWallet.GoldWallet", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Balance")
                        .HasPrecision(18, 6)
                        .HasColumnType("TEXT");

                    b.Property<int>("ConcurrencyToken")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("LockedBalance")
                        .HasPrecision(18, 6)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("Wallets", null, t =>
                        {
                            t.HasCheckConstraint("CK_Wallets_Balance", "CAST(Balance AS NUMERIC) >= 0");

                            t.HasCheckConstraint("CK_Wallets_LockedBalance", "CAST(LockedBalance AS NUMERIC) >= 0 AND CAST(LockedBalance AS NUMERIC) <= CAST(Balance AS NUMERIC)");
                        });
                });

            modelBuilder.Entity("GoldLedger.Domain.Aggregates.Payments.PaymentSettlement", b =>
                {
                    b.Property<Guid>("OrderId")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Amount")
                        .HasPrecision(18, 2)
                        .HasColumnType("TEXT");

                    b.Property<int>("OrderType")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("SettledAt")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("OrderId");

                    b.HasIndex("UserId");

                    b.ToTable("PaymentSettlements", null, t =>
                        {
                            t.HasCheckConstraint("CK_PaymentSettlements_Amount", "CAST(Amount AS NUMERIC) >= 0");

                            t.HasCheckConstraint("CK_PaymentSettlements_OrderType", "OrderType IN (1, 2)");
                        });
                });

            modelBuilder.Entity("GoldLedger.Infrastructure.Persistence.DomainEventRecord", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Payload")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.ToTable("DomainEventRecords", (string)null);
                });

            modelBuilder.Entity("GoldLedger.Domain.Aggregates.GoldOrder.GoldOrder", b =>
                {
                    b.OwnsMany("GoldLedger.Domain.ValueObjects.Tag", "Tags", b1 =>
                        {
                            b1.Property<Guid>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("TEXT");

                            b1.Property<Guid>("OrderId")
                                .HasColumnType("TEXT");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasMaxLength(50)
                                .HasColumnType("TEXT");

                            b1.HasKey("Id");

                            b1.HasIndex("OrderId");

                            b1.ToTable("OrderTags", null, t =>
                                {
                                    t.HasCheckConstraint("CK_OrderTags_Value", "length(trim(Value)) BETWEEN 1 AND 50");
                                });

                            b1.WithOwner()
                                .HasForeignKey("OrderId");
                        });

                    b.Navigation("Tags");
                });

            modelBuilder.Entity("GoldLedger.Domain.Aggregates.Payments.PaymentSettlement", b =>
                {
                    b.HasOne("GoldLedger.Domain.Aggregates.GoldOrder.GoldOrder", null)
                        .WithOne()
                        .HasForeignKey("GoldLedger.Domain.Aggregates.Payments.PaymentSettlement", "OrderId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
