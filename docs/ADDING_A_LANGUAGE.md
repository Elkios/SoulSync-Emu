# Adding a language

SoulSync is multilingual. Translating the interface needs **no code** — just one JSON file. 🌍

Locales live in [`ui/locales/`](../ui/locales). English ([`en.json`](../ui/locales/en.json)) is
the **source of truth**.

---

## Steps

1. **Copy** `ui/locales/en.json` to `ui/locales/<code>.json`, where `<code>` is the language's
   [BCP-47 code](https://en.wikipedia.org/wiki/IETF_language_tag) (`es`, `de`, `pt-BR`, `ja`…).

2. **Edit the `_meta`** block:
   ```json
   "_meta": { "name": "Español", "code": "es", "authors": ["yourname"] }
   ```

3. **Translate the values** (right side only). **Keep the keys** and any `{placeholders}`:
   ```json
   "notify.caught": "¡{name} ha sido capturado!"
   ```
   - Don't translate keys (`notify.caught`).
   - Keep `{name}` and other `{...}` placeholders — they're filled in at runtime.
   - Keep it punchy and gamer-friendly, matching the [tone of voice](BRAND.md#tone-of-voice).

4. **Register** your language in [`ui/locales/index.json`](../ui/locales/index.json):
   ```json
   { "default": "en", "available": ["en", "fr", "es"] }
   ```

5. Open a **pull request** against `soulsync`. That's it!

---

## Rules

- ✅ Every key in `en.json` should exist in your file (missing keys fall back to English).
- ✅ Valid JSON, UTF-8, no trailing commas.
- ❌ Don't change keys or remove `{placeholders}`.

> Engine-level strings (a few backend messages) live in `SoulSync.Core` resources and are a
> separate, optional contribution. The UI locale file above covers everything players see.

Merci / thanks / gracias for making SoulSync speak your language! 🔗
