namespace Tedwren.DataAccess.Connections;

/// <summary>
/// Creates ADO.NET connections for the <b>Commercial</b> database — the second, separate catalogue that holds
/// the admin/commercial plane (subscriptions, payments, mandates, payouts, webhook events, launch list, leads,
/// affiliates). Kept distinct from <see cref="IDbConnectionFactory"/> (the product/compliance database) so a
/// repository is unambiguous about which database it opens: a commercial repository takes this factory, a
/// product repository takes the plain one. Both share the <see cref="Dialects.ISqlDialect"/> engine.
/// </summary>
public interface IAdminDbConnectionFactory : IDbConnectionFactory
{
}
