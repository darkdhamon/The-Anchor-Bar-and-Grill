# The Anchor Bar and Grill

This repository currently contains the first code-native website mockup for The Anchor Bar & Grill in a Blazor Server application.

## Current Mockup Scope

- Guest-facing homepage with a welcome-first layout that is separate from the menu
- Menu mockup page inspired by the existing printed menu
- Optional menu-item image support in the menu mockup and menu admin mockup
- Optional offer start/end dates for menu items, including mockup states for seasonal, limited time, and coming soon items
- Events mockup page for recurring nights and featured programming
- About mockup page for the restaurant story and guest experience
- Contact mockup page for location, phone, hours, and guest inquiry layout
- Admin mockup pages for managing events, menu items, about content, and contact details
- Shared light and dark themes using the menu-inspired nautical palette

## Design Direction

- Uses the printed menu as the primary visual reference for colors, tone, and structure
- Reuses the existing Anchor logo inside the shared site shell
- Applies a warm paper-like light theme, a navy-forward dark theme, and bold blue framing elements
- Wires the shared header theme toggle so reviewers can switch the live mockup between light and dark themes
- Matches the menu typography direction with `Bebas Neue`, `Patrick Hand`, and `Barlow Condensed`

## Development Notes

- The development configuration now includes a LocalDB connection string so the mockup can run locally without additional secret setup
- The current homepage uses a styled building-photo placeholder until a real exterior image is added to the project
- Starter scaffold pages such as `Counter`, `Weather`, and other unused sample content have been removed from the public experience

