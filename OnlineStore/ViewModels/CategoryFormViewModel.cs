using System.ComponentModel.DataAnnotations;

namespace OnlineStore.ViewModels;

/// <summary>分类创建/编辑表单视图模型。</summary>
public class CategoryFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "CategoryNameRequired")]
    [StringLength(100)]
    [Display(Name = "分类名称")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "分类描述")]
    public string? Description { get; set; }

    [Display(Name = "排序")]
    public int DisplayOrder { get; set; }
}
