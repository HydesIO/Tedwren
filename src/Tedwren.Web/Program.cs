using System.Text.Unicode;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.WebEncoders;
using Tedwren.Web.Analytics;
using Tedwren.Web.Configuration;
using Tedwren.Web.Content;
using Tedwren.Web.Controllers;
using Tedwren.Web.Leads;
using Tedwren.Web.Partners;

// Composition root for Tedwren.Web — the public, server-rendered marketing site (Web Plan §2).
// It is an ASP.NET Core MVC deployable, separate from the product API and Blazor client. Through W7 it
// carries the shared chrome, the content layer, the core/product/legal pages, lead capture (with
// anti-bot + routing), cookie consent + analytics gating, and the approval-gated partner programme.

var builder = WebApplication.CreateBuilder(args);

// Site navigation structure (which pages exist, in what order) is app config, bound from the "Site"
// section. Customer-visible copy/identity comes from the content layer below, not from here.
builder.Services.Configure<SiteConfig>(builder.Configuration.GetSection(SiteConfig.SectionName));

// Content layer (Web Plan §3): the JSON-backed IContentProvider is the source of truth for every
// customer-visible string — brand, legal entity, product names, prices, copy — so changes need no
// redeploy. A headless CMS can replace it behind the same interface later with no view changes.
builder.Services.AddJsonContent(builder.Configuration, builder.Environment.ContentRootPath);

// Let Razor emit real Unicode (the £ symbol, em dashes, curly quotes) instead of numeric entities.
// The HTML-sensitive characters (< > & " ') are still encoded, so output encoding stays safe.
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(UnicodeRanges.All));

// Lead capture (Web Plan §7): options + the routing seam. LoggingLeadRouter is the launch
// implementation; a CRM/email integration replaces it behind ILeadRouter with no controller changes.
builder.Services.Configure<LeadOptions>(builder.Configuration.GetSection(LeadOptions.SectionName));
builder.Services.AddScoped<ILeadRouter, LoggingLeadRouter>();

// Analytics (Web Plan §8): GA4 is off unless configured AND consented to. Empty id by default.
builder.Services.Configure<AnalyticsOptions>(builder.Configuration.GetSection(AnalyticsOptions.SectionName));

// Partner programme (Web Plan §7): approval-gated applications, referral attribution and clawback,
// backed by an in-memory store seam (a database-backed store replaces it behind IPartnerStore later).
builder.Services.AddPartnerProgramme(builder.Configuration);

// Rate limiting on the public form POST endpoints (Web Plan §7), alongside antiforgery + honeypot.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(LeadController.RateLimitPolicy, limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

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

// Pre-launch gate: when Site:IsLanding is on, funnel the whole site to the landing page. Placed after
// static files (so assets load) and before routing (so the rewritten path is routed to the landing).
app.UseMiddleware<Tedwren.Web.Configuration.LandingGateMiddleware>();

app.UseRouting();
app.UseRateLimiter();

app.MapControllers();

app.Run();

/// <summary>Exposed so the integration test host (Tedwren.Web.Tests) can bootstrap the site.</summary>
public partial class Program;
