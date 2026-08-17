using EingabeAusgabeRechner.Data;
using Microsoft.EntityFrameworkCore;

namespace EingabeAusgabeRechner.Services;

public class CategoryService(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    public async Task<List<Category>> GetCategoriesAsync(string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Categories
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetCategoryAsync(int id, string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
    }

    public async Task<Category> CreateCategoryAsync(Category category)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    public async Task UpdateCategoryAsync(Category category)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Categories.Update(category);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Returns the number of transactions still using this category.
    /// </summary>
    public async Task<int> GetTransactionCountAsync(int categoryId, string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Transactions.CountAsync(t => t.CategoryId == categoryId && t.UserId == userId);
    }

    public async Task DeleteCategoryAsync(int id, string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (category is null) return;
        db.Categories.Remove(category);
        await db.SaveChangesAsync();
    }
}
