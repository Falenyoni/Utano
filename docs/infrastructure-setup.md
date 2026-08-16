# Infrastructure Setup — Domain, DNS, Email

Covers the company domain, DNS records, and the Resend email setup. For file storage (Cloudflare R2), see `docs/file-storage.md` — not duplicated here.

---

## Domain

**`usenemihealth.com`** — registered via Cloudflare Registrar (no markup, same account already used for R2), 2026-08-16.

**Brand vs legal entity:** the product/brand is **Utano** for public-facing content, but `utano.com` was unavailable and several close variants (`utano.app`, `getutano.com`, `echo`/`nemi`-prefixed alternatives) turned out to collide with existing companies in health tech specifically — most notably **Utano Africa** (Harare, medical equipment distribution — same country, same industry) and **Utano Health Ltd** (Uganda, wellness services). `usenemihealth.com` was chosen instead, derived from the legal entity name (New Echo Medical Investments → NEMI). The registered company name itself stays reserved for legal/registrant purposes (WHOIS, Meta Business verification, aggregator KYB) — see `CONTEXT.md` / project memory for the brand-vs-entity distinction if the public product name changes again later.

**Planned subdomain structure** (not yet live):

| Subdomain | Purpose | Replaces |
|---|---|---|
| `app.usenemihealth.com` | Frontend | `utano-frontend.onrender.com` |
| `api.usenemihealth.com` | Backend API | current Render API URL |
| `usenemihealth.com` (apex) | Marketing page later, or redirect to `app.` | — |

Email sends from the apex (`notifications@usenemihealth.com`) rather than a dedicated `mail.` subdomain — simpler DNS, and the isolation a dedicated sending subdomain buys isn't worth the extra layer until there's a separate public website with its own reputation to protect.

**Render custom domains require a paid instance type per service** (confirmed from Render's docs — not available on free tier), separate from the workspace plan. The Hobby workspace plan already includes 2 custom domain slots, which covers `app.` + `api.`, but each of the API and frontend services individually needs to be moved off a free instance before a custom domain can attach to it.

**When `app.`/`api.` go live, these need updating too:**
- API's `Cors:AllowedOrigins` config (Render env var `Cors__AllowedOrigins__N`) — add `https://app.usenemihealth.com`
- R2 bucket (`utano-files`) CORS policy — add the same origin
- Frontend's `VITE_API_BASE_URL` build env var — point at `https://api.usenemihealth.com`

---

## Email — Resend

**Why Resend:** matches the existing Render/Cloudflare-style stack, generous free tier (3,000/month, 100/day) — plenty for pilot-stage volume. Considered and ruled out: Postmark (best transactional deliverability but no meaningful free tier), SendGrid (heavier setup, inconsistent deliverability reputation), AWS SES (cheapest at scale but starts sandboxed, colder DX), MailerLite/MailerSend (MailerLite is a marketing-campaigns tool, not transactional — MailerSend is their separate transactional product, viable but comes with a marketing-tool ecosystem Utano doesn't need yet).

### Domain verification

Added manually in Cloudflare DNS rather than using Resend's "Auto configure" — that option requests an OAuth-style grant for Resend to write DNS records directly on the Cloudflare account, which isn't worth trading for a few minutes of copy-pasting on a domain this central to the company's infrastructure.

Records added (`usenemihealth.com`, Resend region: Ireland eu-west-1):

| Type | Name | Content | Purpose |
|---|---|---|---|
| TXT | `resend._domainkey` | `p=MIGfMA0GCSqG...` (DKIM public key, see Resend dashboard for the current value) | DKIM signing |
| MX | `send` | `feedback-smtp.eu-west-1.amazonses.com` (priority 10) | Bounce/feedback handling |
| TXT | `send` | `v=spf1 include:amazonses.com ~all` | SPF |
| TXT | `_dmarc` | `v=DMARC1; p=none;` | DMARC (monitoring only — `p=none` doesn't reject/quarantine anything yet) |

Verified 2026-08-16. Status confirmed in Resend: Domain Verified, DKIM Verified.

### API key

Created scoped to **Sending access only** (not full account access) and to **`usenemihealth.com` specifically** (not "All domains") — least privilege in both dimensions. Only matters in practice once there's a second domain on the account, but costs nothing to do now.

Stored as:
- Local: `dotnet user-secrets set "Resend:ApiKey" "..."` (in `Utano.API`)
- Render: env var `Resend__ApiKey`

### Not yet built

The credential and domain are ready, but no code sends through Resend yet. Needed for:
- **#32** Forgot Password — request-reset endpoint, email send, complete-reset endpoint
- **#14** Appointment reminders — wiring an actual send into the existing `AppointmentReminderScanJob`, which currently runs on schedule with nothing to send through

See `docs/technical-debt-and-priorities.md` (#32, #14) and `docs/build-plan-2026-08.md` for status.

---

## SMS / WhatsApp

Not yet set up. Provider options researched for Zimbabwe coverage: Econet's own A2P API (best rates if most patients are on Econet, cross-network reach to NetOne/Telecel unconfirmed), or an aggregator with all-three-network coverage in one API (EasySendSMS, TextPeak, Africala were the candidates found). WhatsApp goes through a BSP (Twilio, 360dialog, or an aggregator that also routes WhatsApp) under one shared Utano-owned WhatsApp Business Account, with practice name as a template variable rather than a per-practice account — see the SaaS multi-tenant messaging architecture note below.

## Multi-tenant messaging architecture

One shared sending identity per channel, owned by the company, not one per practice-client:

- **Email:** one domain/address (`usenemihealth.com`), practice name in message content, `Reply-To` set to the practice's own `Practice.ContactEmail` so patient replies reach the clinic directly
- **SMS:** one Sender ID initially (practice name in the message body); per-practice Sender IDs possible later if it becomes a differentiator, but each needs its own carrier approval
- **WhatsApp:** one WhatsApp Business Account under the company's Meta Business verification, practice name as a template variable

Small/solo practice-clients can't realistically manage their own domain, DNS, or Meta Business verification — personalization happens at the content level, not the infrastructure level. Trade-off worth knowing: deliverability reputation is shared across all practices on a shared sender, so one badly-behaving practice's patients marking messages as spam could affect everyone else on the platform. Not urgent to solve before there are multiple real paying customers.
