using EingabeAusgabeRechner.Data;
using Microsoft.EntityFrameworkCore;

namespace EingabeAusgabeRechner.Services;

public class TransactionService(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    public async Task<List<Transaction>> GetTransactionsAsync(
        string userId,
        string? searchTerm = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var query = db.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(t => t.Description.Contains(searchTerm));

        if (dateFrom.HasValue)
            query = query.Where(t => t.Date >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(t => t.Date <= dateTo.Value);

        return await query.OrderByDescending(t => t.Date).ToListAsync();
    }

    public async Task<List<Transaction>> GetTop5Async(string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        // Fetch data to client first due to SQLite limitation with decimal ordering
        var transactions = await db.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId)
            .ToListAsync();

        return transactions
            .OrderByDescending(t => t.Amount)
            .Take(5)
            .ToList();
    }

    public async Task<Transaction?> GetTransactionAsync(int id, string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Transactions
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    }

    public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        return transaction;
    }

    public async Task UpdateTransactionAsync(Transaction transaction)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Transactions.Update(transaction);
        await db.SaveChangesAsync();
    }

    public async Task DeleteTransactionAsync(int id, string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (transaction is null) return;
        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync();
    }

    public async Task<(decimal TotalIncome, decimal TotalExpense)> GetSummaryAsync(string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        // Fetch data to client first due to SQLite limitation with decimal aggregation
        var transactions = await db.Transactions
            .Where(t => t.UserId == userId)
            .Select(t => new { t.Type, t.Amount })
            .ToListAsync();

        var totals = transactions
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount) })
            .ToList();

        var income = totals.FirstOrDefault(x => x.Type == TransactionType.Income)?.Total ?? 0m;
        var expense = totals.FirstOrDefault(x => x.Type == TransactionType.Expense)?.Total ?? 0m;
        return (income, expense);
    }
}
