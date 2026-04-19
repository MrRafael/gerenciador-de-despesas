using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MyFinBackend.Model;

namespace MyFinBackend.Database;

public partial class FinanceContext : DbContext
{
    public FinanceContext(DbContextOptions<FinanceContext> options)
        : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<ExpenseCategory> ExpenseCategories { get; set; }
    public virtual DbSet<Expense> Expenses { get; set; }

    public virtual DbSet<Group> Groups { get; set; }
    public virtual DbSet<GroupMember> GroupMembers { get; set; }
    public virtual DbSet<GroupMemberConfig> GroupMemberConfigs { get; set; }
    public virtual DbSet<GroupSplitConfig> GroupSplitConfigs { get; set; }
    public virtual DbSet<GroupSplitConfigShare> GroupSplitConfigShares { get; set; }
    public virtual DbSet<GroupMonthClose> GroupMonthCloses { get; set; }
    public virtual DbSet<GroupMonthCloseConfirmation> GroupMonthCloseConfirmations { get; set; }
    public virtual DbSet<ExpenseSplitConfig> ExpenseSplitConfigs { get; set; }
    public virtual DbSet<ExpenseSplitShare> ExpenseSplitShares { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_pk");

            entity.ToTable("User");

            entity.Property(e => e.Id)
                .HasColumnType("character varying")
                .HasColumnName("id");
            entity.Property(e => e.Email)
                .HasColumnType("character varying")
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.Photo)
                .HasColumnType("character varying")
                .HasColumnName("photo");

            entity.HasMany(u => u.ExpenseCategories)
                .WithOne(ec => ec.User)
                .HasForeignKey(ec => ec.UserId)
                .IsRequired();

            entity.HasMany(u => u.OwnedGroups)
                .WithOne(g => g.User)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_user_owned_groups");

            entity.HasMany(u => u.GroupMemberships)
                .WithOne(gm => gm.User)
                .HasForeignKey(gm => gm.UserId)
                .HasConstraintName("fk_user_group_memberships");

            entity.HasMany(u => u.Expenses)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_expense_user");
        });

        modelBuilder.Entity<ExpenseCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("expense_category_pk");

            entity.ToTable("ExpenseCategory");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");

            entity.Property(e => e.UserId)
                .HasColumnType("character varying")
                .HasColumnName("user_id");

            entity.HasOne(d => d.User)
                .WithMany(p => p.ExpenseCategories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_expense_category_user");

            entity.HasMany(ec => ec.Expenses)
                .WithOne(e => e.Category)
                .HasForeignKey(e => e.CategoryId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_expense_category");
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("expense_pk");

            entity.ToTable("Expense");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");

            entity.Property(e => e.Value)
                .HasColumnType("numeric")
                .HasColumnName("value");

            entity.Property(e => e.Date)
                .HasColumnType("date")
                .HasColumnName("date");

            entity.Property(e => e.UserId)
                .HasColumnType("character varying")
                .HasColumnName("user_id");

            entity.Property(e => e.CategoryId)
                .HasColumnName("category_id");

            entity.Property(e => e.GroupId)
                .HasColumnName("group_id")
                .IsRequired(false);

            entity.HasOne(e => e.Group)
                .WithMany()
                .HasForeignKey(e => e.GroupId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_expense_group");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("group_pk");
            entity.ToTable("Group");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name")
                .IsRequired();

            entity.Property(e => e.UserId)
                .HasColumnType("character varying")
                .HasColumnName("user_id");

            entity.Property(e => e.IsDeleted)
                .HasColumnName("is_deleted")
                .HasDefaultValue(false);

            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(d => d.User)
                .WithMany(x => x.OwnedGroups)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_group_owner");
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => new { e.GroupId, e.UserId }).HasName("group_member_pk");

            entity.ToTable("GroupMember");

            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("character varying");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            entity.HasOne(d => d.Group)
                .WithMany(x => x.Members)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_member_group");

            entity.HasOne(d => d.User)
                .WithMany(x => x.GroupMemberships)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_member_user");
        });

        modelBuilder.Entity<GroupMemberConfig>(entity =>
        {
            entity.HasKey(e => new { e.GroupId, e.UserId }).HasName("group_member_config_pk");

            entity.ToTable("GroupMemberConfig");

            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("character varying");
            entity.Property(e => e.Salary).HasColumnName("salary").HasColumnType("numeric").IsRequired(false);

            entity.HasOne(e => e.Group)
                .WithMany()
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_group_member_config_group");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_group_member_config_user");
        });

        modelBuilder.Entity<GroupSplitConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("group_split_config_pk");

            entity.ToTable("GroupSplitConfig");

            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.SplitType).HasColumnName("split_type");
            entity.Property(e => e.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);

            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(e => e.Group)
                .WithMany()
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_group_split_config_group");
        });

        modelBuilder.Entity<GroupSplitConfigShare>(entity =>
        {
            entity.HasKey(e => new { e.GroupSplitConfigId, e.UserId }).HasName("group_split_config_share_pk");

            entity.ToTable("GroupSplitConfigShare");

            entity.Property(e => e.GroupSplitConfigId).HasColumnName("group_split_config_id");
            entity.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("character varying");
            entity.Property(e => e.Percentage).HasColumnName("percentage").HasColumnType("numeric");

            entity.HasOne(e => e.GroupSplitConfig)
                .WithMany(c => c.Shares)
                .HasForeignKey(e => e.GroupSplitConfigId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_split_config_share_config");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_split_config_share_user");
        });

        modelBuilder.Entity<GroupMonthClose>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("group_month_close_pk");

            entity.ToTable("GroupMonthClose");

            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.Month).HasColumnName("month");
            entity.Property(e => e.Year).HasColumnName("year");
            entity.Property(e => e.ClosedAt).HasColumnName("closed_at").IsRequired(false);

            entity.HasOne(e => e.Group)
                .WithMany()
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_group_month_close_group");
        });

        modelBuilder.Entity<GroupMonthCloseConfirmation>(entity =>
        {
            entity.HasKey(e => new { e.GroupMonthCloseId, e.UserId }).HasName("group_month_close_confirmation_pk");

            entity.ToTable("GroupMonthCloseConfirmation");

            entity.Property(e => e.GroupMonthCloseId).HasColumnName("group_month_close_id");
            entity.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("character varying");
            entity.Property(e => e.ConfirmedAt).HasColumnName("confirmed_at");

            entity.HasOne(e => e.GroupMonthClose)
                .WithMany(c => c.Confirmations)
                .HasForeignKey(e => e.GroupMonthCloseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_month_close_confirmation_close");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_month_close_confirmation_user");
        });

        modelBuilder.Entity<ExpenseSplitConfig>(entity =>
        {
            entity.HasKey(e => e.ExpenseId).HasName("expense_split_config_pk");

            entity.ToTable("ExpenseSplitConfig");

            entity.Property(e => e.ExpenseId).HasColumnName("expense_id");
            entity.Property(e => e.GroupSplitConfigId).HasColumnName("group_split_config_id");

            entity.HasOne(e => e.Expense)
                .WithOne()
                .HasForeignKey<ExpenseSplitConfig>(e => e.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_expense_split_config");

            entity.HasOne(e => e.GroupSplitConfig)
                .WithMany()
                .HasForeignKey(e => e.GroupSplitConfigId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_expense_split_config_group_split");
        });

        modelBuilder.Entity<ExpenseSplitShare>(entity =>
        {
            entity.HasKey(e => new { e.ExpenseId, e.UserId }).HasName("expense_split_share_pk");

            entity.ToTable("ExpenseSplitShare");

            entity.Property(e => e.ExpenseId).HasColumnName("expense_id");
            entity.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("character varying");
            entity.Property(e => e.Percentage).HasColumnName("percentage").HasColumnType("numeric");
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("numeric");

            entity.HasOne(e => e.Expense)
                .WithMany()
                .HasForeignKey(e => e.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_split_share_expense");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_split_share_user");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
