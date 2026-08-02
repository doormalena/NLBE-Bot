# Discord Application Setup Guide

*Production & Development Configuration*

This document describes how to configure a Discord Developer Application for NLBE‑Bot.
Use this guide when creating a new application, migrating from a legacy bot, or onboarding new contributors.

---

## 1. Overview

NLBE‑Bot is a Discord bot designed for the NLBE community.
This guide documents all Discord Developer Portal settings required to recreate the bot’s identity, permissions, OAuth2 configuration, and gateway behavior.

---

## 2. General Information

**Application Name:** NLBE-Bot (or NLBE-BOT (dev/test)
**Team**: NLBE-Bot team
**Application ID:** Generated automatically.  
**Public Key:** Generated automatically. Required for slash command interactions.  
**Interactions Endpoint URL:** Leave empty unless you implement HTTP-based interactions. NLBE‑Bot uses Gateway interactions.  
**Linked Roles Verification URL:** Leave empty unless Linked Roles are implemented.  
**Terms of Service URL:** Optional but recommended.  
**Privacy Policy URL:** Optional but recommended.

---

## 3. Installation Settings

### Installation Contexts
- User Install: OFF
- Guild Install: ON

### Default Install Settings

**Guild Install**

**Scopes:**  
- applications.commands  
- bot  

**Permissions:**  
- Add Reactions  
- Attach Files  
- Change Nickname  
- Connect  
- Embed Links  
- Manage Messages  
- Manage Nicknames  
- Manage Roles  
- Mention Everyone  
- Moderate Members  
- Read Message History  
- Send Messages  
- Use External Emojis  
- Use Slash Commands  
- View Channels  

---

## 4. OAuth2 Settings

**Client Information:**  
- Client ID: auto-generated  
- Client Secret: keep private  

**Redirects:** Leave empty unless you implement OAuth2 login flows.

---

## 5. Bot Settings

**Username:** NLBE-Bot (or NLBE-BOT (dev/test)
**Token:** Generate a new token for production. Store it in `.env` as `NLBEBOT:DiscordToken`.  
**Public Bot:** ON  
**Requires OAuth2 Code Grant:** OFF  

### Privileged Gateway Intents
- Presence Intent: OFF  
- Server Members Intent: ON  
- Message Content Intent: ON  

---

## 8. Emojis

NLBE‑Bot does not use custom Emojis. Leave default.

---

## 9. Webhooks (Application Webhooks)

NLBE‑Bot does not use application webhooks. Leave default.

---

## 10. Rich Presence

Not used by NLBE‑Bot. Leave default.

---

## 11. App Testers

Optional — add Discord users who should test the app.

---

## 12. App Verification

Verification is only required if:  
- your bot joins 100+ servers, and  
- you want to scale beyond that limit.

Requirements include:  
- Team ownership  
- Terms of Service URL  
- Privacy Policy URL  
- Verified emails + 2FA  
- Identity verification for team owner  

Production NLBE‑Bot does not require verification as it's dedicated for one server (NLBE).
If you plan to scale, follow Discord’s verification process.
