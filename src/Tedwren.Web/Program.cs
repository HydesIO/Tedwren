using Tedwren.Web.Configuration;
using Tedwren.Web.Content;

// Composition root for Tedwren.Web — the public, server-rendered marketing site (Web Plan §2).
// It is an ASP.NET Core MVC deployable, separate from the product API and Blazor client. W1 stands
// up the skeleton: shared layout + tokens, the SiteHeader/SiteFooter/Cta components, and routing for
// every page in the sitemap returning stub views. Content, lead capture, consent and the partner
// programme arrive in later W-phases.

var builder = WebApplication.CreateBuilder(args);

// Site navigation structure (which pages exist, in what order) is app config, bound from the "Site"
// section. Customer-visible copy/identity comes from the content layer below, not from here.
builder.Services.Configure<SiteConfig>(builder.Configuration.GetSection(SiteConfig.SectionName));

// Content layer (Web Plan §3): the JSON-backed IContentProvider is the source of truth for every
// customer-visible string — brand, legal entity, product names, prices, copy — so changes need no
// redeploy. A headless CMS can replace it behind the same interface later with no view changes.
builder.Services.AddJsonContent(builder.Configuration, builder.Environment.ContentRootPath);

builder.Services.AddControllersWithViews();

var app = builder.Build();

// The marketing site serves only public pages, so — unlike the API — there is no authentication and
// no fallback authorization policy here.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Unknown routes re-execute the friendly error page (Web Plan §4: 404 → Return home / Book a demo).
app.UseStatusCodePagesWithReExecute("/error");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllers();

app.Run();

/// <summary>Exposed so the integration test host (Tedwren.Web.Tests) can bootstrap the site.</summary>
public partial class Program;
