using System.Text.Json;
using g19_sep490_ealds.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Utils;

public static class PurchaseRequestLineHelper
{
    public static async Task<List<AssetRequestPurchaseLine>> EnsureLinesAsync(
        EaldsDbContext context,
        AssetRequest ar,
        CancellationToken cancellationToken = default)
    {
        var existing = await context.AssetRequestPurchaseLines
            .Where(l => l.AssetRequestId == ar.AssetRequestId)
            .OrderBy(l => l.LineIndex)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
            return existing;

        var equipment = ParseEquipment(ar.ProposedData);
        if (equipment.Count == 0)
            return existing;

        var index = 0;
        foreach (var row in equipment)
        {
            context.AssetRequestPurchaseLines.Add(new AssetRequestPurchaseLine
            {
                AssetRequestId = ar.AssetRequestId,
                LineIndex = index++,
                ItemName = row.Name,
                Quantity = row.Quantity < 1 ? 1 : row.Quantity,
                Unit = row.Unit,
                ModelCode = row.ModelCode,
                EstimatedPrice = row.EstimatedPrice,
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return await context.AssetRequestPurchaseLines
            .Where(l => l.AssetRequestId == ar.AssetRequestId)
            .OrderBy(l => l.LineIndex)
            .ToListAsync(cancellationToken);
    }

    private static List<EquipmentRow> ParseEquipment(string? proposedData)
    {
        var list = new List<EquipmentRow>();
        if (string.IsNullOrWhiteSpace(proposedData))
            return list;

        try
        {
            using var doc = JsonDocument.Parse(proposedData);
            if (!doc.RootElement.TryGetProperty("equipment", out var equipment) ||
                equipment.ValueKind != JsonValueKind.Array)
                return list;

            foreach (var item in equipment.EnumerateArray())
            {
                var name = item.TryGetProperty("name", out var n) ? n.GetString()?.Trim() : null;
                var assetTypeName = item.TryGetProperty("assetTypeName", out var atn) ? atn.GetString()?.Trim() : null;
                var displayName = !string.IsNullOrWhiteSpace(name) ? name : assetTypeName;
                if (string.IsNullOrWhiteSpace(displayName))
                    continue;

                var qty = 1;
                if (item.TryGetProperty("quantity", out var q))
                {
                    if (q.ValueKind == JsonValueKind.Number && q.TryGetInt32(out var qi))
                        qty = qi < 1 ? 1 : qi;
                }

                var unit = item.TryGetProperty("unit", out var u) ? u.GetString() : null;
                var modelCode =
                    item.TryGetProperty("modelCode", out var mc) ? mc.GetString() :
                    item.TryGetProperty("machineCode", out var legacyMc) ? legacyMc.GetString() : null;
                var est = item.TryGetProperty("estimatedPrice", out var e)
                    ? e.ValueKind == JsonValueKind.String ? e.GetString() : e.GetRawText()
                    : null;

                list.Add(new EquipmentRow(displayName, qty, unit, modelCode, est));
            }
        }
        catch
        {
            // ignore malformed JSON
        }

        return list;
    }

    private sealed record EquipmentRow(
        string Name,
        int Quantity,
        string? Unit,
        string? ModelCode,
        string? EstimatedPrice);
}
