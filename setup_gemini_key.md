# Gemini API Key Setup Guide

## Current Issue
- Free tier quota: 20 requests/day per model
- All models failing: gemini-3.1-pro-preview, gemini-3-pro, gemini-2.5-pro, gemini-2.0-flash, gemini-2.5-flash
- Error: "RESOURCE_EXHAUSTED" - Quota exceeded

## Solution: Enable Billing

### Step 1: Go to Google AI Studio
1. Visit https://aistudio.google.com
2. Sign in with your Google account
3. Click "Get API Key" in the left menu

### Step 2: Enable Billing
1. In Google AI Studio, go to "API keys" section
2. Click on your existing key or create a new one
3. Click "Enable billing" 
4. Add a payment method (credit/debit card)
5. You'll get $300 free credit for new users + pay-as-you-go after

### Step 3: Configure Usage Limits (Optional but Recommended)
1. In API key settings, set daily/monthly limits
2. Recommended: $10/day limit to prevent unexpected charges
3. Pricing (as of 2024):
   - Gemini Pro: $0.00025 per 1K characters
   - Gemini Flash: $0.000075 per 1K characters
   - Very affordable for travel planning use

### Step 4: Copy Your API Key
- Copy the new API key (starts with "AIza...")
- This key will have much higher quotas

## Update Your App Settings

Add/update in `appsettings.Development.json`:
```json
{
  "Google": {
    "GeminiApiKey": "AIza...YOUR_NEW_KEY_HERE"
  }
}
```

## Restart Your API
```bash
cd D:\Planora\PlanoraApp.API
dotnet run
```

## Verify
- Try generating a trip again
- Check console for successful Gemini responses
- No more "TooManyRequests" errors

## Important Notes
- The free tier is only for testing/development
- Production apps need billing enabled
- With $300 free credit, you can test extensively
- Monitor usage at https://aistudio.google.com/usage
