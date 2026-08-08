using Tedwren.Application.Inductions;
using Tedwren.Application.Persistence;

namespace Tedwren.DataAccess.Inductions;

/// <summary>
/// Seeds the default induction template (MC-3) into the database after migrations, keeping
/// <see cref="DefaultInductionTemplate"/> as the single source of truth rather than duplicating it in SQL.
/// Idempotent: the template is inserted only when its fixed id is absent, so it is safe to run at every
/// startup.
/// </summary>
public sealed class InductionTemplateSeeder
{
    private readonly IInductionTemplateRepository _templates;

    /// <summary>Creates the seeder over the template repository.</summary>
    public InductionTemplateSeeder(IInductionTemplateRepository templates) => _templates = templates;

    /// <summary>Inserts the default template when it is not already present.</summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (await _templates.GetByIdAsync(DefaultInductionTemplate.TemplateId, cancellationToken) is null)
        {
            await _templates.AddAsync(DefaultInductionTemplate.Template, cancellationToken);
        }
    }
}
