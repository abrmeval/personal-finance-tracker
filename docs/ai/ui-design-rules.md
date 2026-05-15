# Personal Finance Tracker UI Design Rules

## General Principles
Every design decision, component, and layout MUST consider **mobile first**.

### Mandatory Principles

1. Design from mobile upward — NEVER desktop-first
2. Minimum viewport: 320px
3. Touch-first interactions — minimum tap target 44×44px
4. Use `100dvh` not `100vh` (mobile browser chrome compatibility)
5. Progressive enhancement — add features for larger screens

### MUST / MUST NOT

- ✅ Start from `xs` breakpoint, add `md`/`lg` overrides
- ✅ `sx={{ fontSize: { xs: '14px', md: '16px' } }}`
- ✅ Test in Chrome DevTools mobile view before desktop
- ❌ Assume desktop layout as baseline
- ❌ Use `100vh` (causes issues with mobile navigation bars)
- ❌ Create horizontal scroll on mobile
- ❌ Use hover-only interactions

## 1. Breakpoints

```javascript
{
  xs: 0,      // Mobile (320px+)
  sm: 600,    // Large mobile / tablet portrait
  md: 900,    // Tablet landscape
  lg: 1200,   // Desktop
  xl: 1536,   // Large desktop
  xxl: 1920,  // Full HD — max content width
}
```
Custom breakpoints can be added  if needed, but `xs` MUST be the default starting point for all styles.

## 2. 4-Layer Container Architecture

Every page section uses this layered system:

```
Layer 0 (always): BackgroundSection   — infinite background (100%, no max-width)
Layer 1 (Always):   SectionContainer    — max 1920px, responsive padding
Layer 2 (always):   ContentContainer    — MUI Grid container with style variants
Layer 3 (optional): GridItem            — MUI Grid item for layout
```

## 3. Image Assets
- Use SVGs for icons and simple graphics
- Use optimized WebP for photos
- Store in `src/assets/` with descriptive names following this pattern:
  - `icon-` prefix for icons + ``descriptive_name`` (e.g. `icon-budget.svg`) 
  - `img-` prefix for photos + ``descriptive_name`` + `_px_resolution` (e.g. `img-dashboard_1920.webp`)

# 4. Accessibility
- All interactive elements MUST have accessible labels (use `aria-label` or visible text)
- Meets WCAG 2.2 AA standards for color contrast and keyboard navigation