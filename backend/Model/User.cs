using System;
using System.Collections.Generic;

namespace MyFinBackend.Model;

public class User
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Photo { get; set; }

    public string Email { get; set; } = null!;

    public ICollection<ExpenseCategory> ExpenseCategories { get; set; } = new List<ExpenseCategory>();

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual ICollection<Group> OwnedGroups { get; set; } = new List<Group>();

    public virtual ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
}
