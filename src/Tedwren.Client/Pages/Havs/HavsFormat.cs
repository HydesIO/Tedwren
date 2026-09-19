namespace Tedwren.Client.Pages.Havs;

/// <summary>Presentation helpers for HAVs exposure bands (PRD §8.2) — maps the band enum name to a readable label.</summary>
public static class HavsFormat
{
    /// <summary>Returns a human-readable label for a HAVs exposure band name (as sent by the API).</summary>
    public static string BandLabel(string band) => band switch
    {
        "BelowActionValue" => "Below action value",
        "AboveActionValue" => "Above action value (EAV)",
        "AboveLimitValue" => "Above limit value (ELV)",
        _ => band,
    };
}
