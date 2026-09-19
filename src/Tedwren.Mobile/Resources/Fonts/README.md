# Fonts

The Tedwren design system uses **Inter** (SIL Open Font License), matching `--font-family` in the web
`tokens.css`. The `.ttf` files are not committed here (to keep the licence and binaries out of the diff).

To bundle them:

1. Download Inter from <https://fonts.google.com/specimen/Inter> or <https://github.com/rsms/inter>.
2. Copy these files into this folder (the csproj already globs `Resources/Fonts/*.ttf`):
   - `Inter-Regular.ttf`   → alias `InterRegular`
   - `Inter-SemiBold.ttf`  → alias `InterSemiBold`
   - `Inter-Bold.ttf`      → alias `InterBold`
3. Uncomment the matching `fonts.AddFont(...)` lines in `MauiProgram.cs`.
4. Keep the OFL licence file alongside the fonts.

Until the fonts are added the app uses the platform default typeface; the tokens, spacing and layout are
unaffected.
