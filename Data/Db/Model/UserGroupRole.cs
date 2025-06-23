using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Db.Model;

[Table("user_group_roles")]
[Index(nameof(UserId), nameof(UserGroupId), IsUnique = true)]
[Index(nameof(UserId), nameof(Role))]
public class UserGroupRole
{
    public int Id { get; init; }

    public required User User { get; init; }
    public int UserId { get; init; }

    public required UserGroup UserGroup { get; init; }
    public int UserGroupId { get; init; }

    public required UserGroupRoleType Role { get; set; }
}

public enum UserGroupRoleType
{
    Owner,
    Admin,
    Member
}

public class UserGroupRoleConfiguration : IEntityTypeConfiguration<UserGroupRole>
{
    public void Configure(EntityTypeBuilder<UserGroupRole> builder)
    {
        builder.HasOne(left => left.User)
            .WithMany()
            .HasForeignKey(left => left.UserId);

        builder.HasOne(left => left.UserGroup)
            .WithMany()
            .HasForeignKey(left => left.UserGroupId);
    }
}