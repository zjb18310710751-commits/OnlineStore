using System.Reflection;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Tests.Services;

public class CatalogSeedSelectionTests
{
    [Fact]
    public void SampleSelection_KeepsFiftyUniqueProductsWithTenPerCategory()
    {
        var categories = Enumerable.Range(1, 5)
            .Select(index => new Category
            {
                Name = $"Category {index}",
                DisplayOrder = index
            })
            .ToList();

        var products = new List<Product>();
        foreach (var category in categories)
        {
            for (var index = 0; index < 12; index++)
            {
                products.Add(new Product
                {
                    Name = $"{category.Name} Product {index}",
                    Categories = new List<Category> { category }
                });
            }
        }

        products[0].Categories.Add(categories[1]);
        products[12].Categories.Add(categories[2]);

        var method = typeof(DbInitializer).GetMethod(
            "SelectBalancedCatalog",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var selected = (List<Product>)method.Invoke(
            null,
            new object[] { products, categories })!;

        Assert.Equal(50, selected.Count);
        Assert.Equal(50, selected.Distinct().Count());
        Assert.All(selected, product => Assert.Single(product.Categories));
        Assert.All(
            categories,
            category => Assert.Equal(10, selected.Count(product => product.Categories.Contains(category))));
    }
}
