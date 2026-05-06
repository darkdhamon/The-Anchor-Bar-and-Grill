# The Anchor Bar and Grill

This repository currently contains the first code-native website mockup for The Anchor Bar & Grill in a Blazor Server application.

## Current Mockup Scope

- Guest-facing homepage with a welcome-first layout that is separate from the menu
- Homepage preview of recurring weekly specials so guests can see day-of-week favorites before opening the full menu
- Homepage list of upcoming events in the next month so guests can plan without leaving the landing page
- Menu mockup page inspired by the existing printed menu
- Weekly recurring specials on the menu mockup for day-of-week traditions like burger nights and dinner specials
- Optional menu-item image support in the menu mockup and menu admin mockup
- Optional offer start/end dates for menu items, including mockup states for seasonal, limited time, and coming soon items
- Native date-picker inputs in the menu admin mockup for offer start and end dates
- Menu admin recurring-specials editor mockup for defining weekly specials separately from one-off offer windows
- Menu admin section assignment mockup that lets staff choose an existing section or type a new one from the same combo-box field
- Events mockup page that lists all upcoming events, including recurring schedules, optional event images, and richer cadence examples such as every other week or the third Friday of the month
- About mockup page for the restaurant story and guest experience
- Contact mockup page for location, phone, hours, dynamic social media links, and guest inquiry layout
- Admin mockup pages for managing events, menu items, about content, and contact details
- Contact admin mockup supports adding, editing, and deleting multiple social media profiles for the public contact page
- Events admin mockup with date/time inputs, richer recurring-event controls, optional images, descriptions, and combo-select promo badges
- Shared light and dark themes using the menu-inspired nautical palette

## Design Direction

- Uses the printed menu as the primary visual reference for colors, tone, and structure
- Reuses the existing Anchor logo inside the shared site shell
- Applies a warm paper-like light theme, a navy-forward dark theme, and bold blue framing elements
- Wires the shared header theme toggle as a compact switch so reviewers can flip the live mockup between light and dark themes
- Matches the menu typography direction with `Bebas Neue`, `Patrick Hand`, and `Barlow Condensed`

## Development Notes

- The development configuration now includes a LocalDB connection string so the mockup can run locally without additional secret setup
- The current homepage uses a styled building-photo placeholder until a real exterior image is added to the project
- The event mockup data now demonstrates weekly, every-other-week, and nth-weekday monthly recurrence patterns so the UI direction can be reviewed before backend scheduling is built
- On mobile, the shared header now keeps public and admin mockup links inside the expandable menu and uses an icon-style site-menu control so it does not compete with the food Menu link
- Non-editor elements such as navigation targets and post-navigation page headings suppress the default browser focus outline, while editor fields keep their normal editing focus behavior
- Account pages continue to use the server-routed Identity flow, so auth navigation should bypass interactive routing when needed
- Starter scaffold pages such as `Counter`, `Weather`, and other unused sample content have been removed from the public experience

