using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Db.Model;

[Table("user_groups")]
public class UserGroup
{
    public int Id { get; set; }
    [StringLength(256)]
    public string? Name { get; set; }
}
