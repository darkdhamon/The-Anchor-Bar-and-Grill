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
- Role-gated admin mockup pages for managing events, menu items, about content, and contact details
- Contact admin mockup supports adding, editing, and deleting multiple social media profiles for the public contact page
- Events admin mockup with date/time inputs, richer recurring-event controls, optional images, descriptions, and combo-select promo badges
- Signed-in-only Help page that explains staff onboarding, roles, bootstrap behavior, and security configuration
- Admin-only User Management page for maintaining staff profile details, setting new temporary passwords for existing users, confirming staff account access, reviewing email-verification status, and assigning or removing `Admin`, `EventManager`, `MenuManager`, and `IT`
- Admin-only Security page for reviewing bootstrap coverage and changing the fallback confirmed-account setting in `appsettings.json`
- Self-service Manage Account profile page where each signed-in user can update their own first name, last name, and phone number
- IT-only placeholder system page that reserves a dedicated technical surface for future diagnostics and tooling
- Shared light and dark themes using the menu-inspired nautical palette

## Staff Access And Security

- Staff accounts are created only by admins; public self-registration is disabled
- Staff sign-in uses local credentials and optional passkeys only; external provider login scaffolding has been removed
- Required application roles are `Admin`, `EventManager`, `MenuManager`, and `IT`
- Authorization is policy-based, with `Admin` acting as the override for general admin, event, and menu management access, while the IT/system page still requires the `IT` role explicitly
- The application seeds missing roles on startup
- A bootstrap account is created only while the site does not yet have at least one `Admin` user and one `IT` user
- The bootstrap account is seeded with both `Admin` and `IT`, is confirmed automatically for sign-in, and must change its password after first successful sign-in before broader access is allowed
- The confirmed-account requirement defaults to `false`
- The confirmed-account rule is controlled by configuration rather than the database
- `AnchorIdentity__RequireConfirmedAccount` acts as the environment-variable override
- `AnchorIdentity:RequireConfirmedAccount` in `appsettings.json` is the admin-editable fallback value
- Admins can manually confirm or unconfirm staff account access because email delivery is still implemented with the no-op sender
- Staff account confirmation is stored separately from email verification, so an approved account can stay active even if the current email address has not been re-verified yet
- Role and account-approval changes are rebuilt from the current database state on the next page refresh, so staff do not need to sign out and back in just to pick up updated access
- Admins can set a new temporary password for an existing user who forgot the current password, and that user is forced back through the password-change flow after the next successful sign-in
- Signed-in users can update their own first name, last name, and phone number from `Manage Account`, using the same validation rules as admin-managed profile updates
- The app prevents removing the last `Admin` assignment or the last `IT` assignment
- An admin cannot remove the `Admin` role from their own signed-in account

## Design Direction

- Uses the printed menu as the primary visual reference for colors, tone, and structure
- Reuses the existing Anchor logo inside the shared site shell
- Applies a warm paper-like light theme, a navy-forward dark theme, and bold blue framing elements
- Wires the shared header theme toggle as a compact switch so reviewers can flip the live mockup between light and dark themes
- Persists the chosen light or dark theme so full-page account routes like `Account/Login` apply the saved theme immediately on load, otherwise defaulting from device theme and then time of day
- Matches the menu typography direction with `Bebas Neue`, `Patrick Hand`, and `Barlow Condensed`

## Development Notes

- The development configuration now includes a LocalDB connection string so the mockup can run locally without additional secret setup
- The current homepage uses a styled building-photo placeholder until a real exterior image is added to the project
- The event mockup data now demonstrates weekly, every-other-week, and nth-weekday monthly recurrence patterns so the UI direction can be reviewed before backend scheduling is built
- On mobile, the shared header now keeps public and admin mockup links inside the expandable menu and uses an icon-style site-menu control so it does not compete with the food Menu link
- The shared header now shows public navigation to everyone, while staff tools only appear after sign-in and are filtered by the current user's roles
- Admin editor controls now share the same themed styling and normalized field sizing across light and dark modes instead of falling back to browser-default inputs
- The login page now uses the same branded themed form treatment as the rest of the site instead of the stock floating-label scaffold
- The public register entry points are disabled, and admins now create staff accounts directly from User Management with a temporary password
- Admins can update each staff member's first name, last name, and phone number from User Management without changing the sign-in email
- Signed-in users can update their own first name, last name, and phone number from Manage Account without waiting on an admin
- Admins can reset an existing staff member's password to a new temporary password from User Management when the staff member forgets it
- Passkey requests now start only after the user clicks the passkey action instead of auto-starting on login-page load
- Register, forced password change, access-denied, and register-confirmation screens now use the same branded account layout as the login page
- Public mockup pages and the staff Help page now opt out of interactive routing so header links behave like normal full page loads from login, contact, and the rest of the guest-facing site
- The shared header theme toggle and mobile menu now use browser-side JavaScript hooks instead of Blazor-only click handlers so they keep working on both static account routes and interactive admin routes
- Internal account and manage-page links now use rooted `/Account/...` routes with the same full-load navigation behavior, so moving between login, recovery, profile, and admin surfaces stays consistent from any page
- Startup bootstrap runs before the request pipeline so roles and the initial administrative account exist before the first login attempt
- Non-editor elements such as navigation targets and post-navigation page headings suppress the default browser focus outline, while editor fields keep their normal editing focus behavior
- Account pages continue to use the server-routed Identity flow, so auth navigation should bypass interactive routing when needed
- Test coverage now spans domain policy/bootstrap logic, repository behavior, layout and page rendering, themed account-route integration, and the full migration chain for the Identity schema
- Starter scaffold pages such as `Counter`, `Weather`, and other unused sample content have been removed from the public experience

