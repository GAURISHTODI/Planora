import json
import os
import sys
import urllib.error
import urllib.request


API_KEY = os.environ.get("GEMINI_API_KEY") or os.environ.get("GOOGLE_GEMINI_API_KEY")
PREFERRED_MODELS = [
    "gemini-2.5-pro",
    "gemini-3.0-flash",
    "gemini-3.1-flash-lite",
    "gemini-2.5-flash",
]


def fetch_json(url: str, data: dict | None = None) -> dict:
    payload = None if data is None else json.dumps(data).encode("utf-8")
    request = urllib.request.Request(
        url,
        data=payload,
        headers={"Content-Type": "application/json"},
        method="GET" if payload is None else "POST",
    )
    with urllib.request.urlopen(request, timeout=20) as response:
        return json.loads(response.read().decode("utf-8"))


def list_models() -> set[str]:
    url = f"https://generativelanguage.googleapis.com/v1beta/models?key={API_KEY}"
    data = fetch_json(url)
    available = set()
    for model in data.get("models", []):
        name = model.get("name", "")
        methods = model.get("supportedGenerationMethods", [])
        if "generateContent" in methods:
            print(name)
            available.add(name.split("/")[-1])
    return available


def test_generation(model: str) -> bool:
    url = f"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={API_KEY}"
    body = {
        "contents": [
            {
                "parts": [
                    {
                        "text": "Reply with exactly: PLANORA_GEMINI_OK",
                    }
                ]
            }
        ],
        "generationConfig": {
            "temperature": 0,
            "maxOutputTokens": 20,
        },
    }
    data = fetch_json(url, body)
    parts = data.get("candidates", [{}])[0].get("content", {}).get("parts", [])
    text = parts[0].get("text", "").strip() if parts else ""
    print(f"\nGeneration test with {model}: {text}")
    return "PLANORA_GEMINI_OK" in text


if not API_KEY:
    print("Set GEMINI_API_KEY or GOOGLE_GEMINI_API_KEY before running this script.", file=sys.stderr)
    sys.exit(1)


try:
    available_models = list_models()
    print("\nPreferred model availability:")
    for model in PREFERRED_MODELS:
        print(f"- {model}: {'available' if model in available_models else 'not listed'}")

    candidate_models = [model for model in PREFERRED_MODELS if model in available_models]
    if not candidate_models:
        print("\nNo preferred Gemini model is currently listed for this API key.", file=sys.stderr)
        sys.exit(2)

    success_model = None
    for chosen_model in candidate_models:
        try:
            if test_generation(chosen_model):
                success_model = chosen_model
                break
        except urllib.error.HTTPError as exc:
            error_body = exc.read().decode("utf-8", errors="replace")
            print(f"\nGeneration test failed for {chosen_model}: HTTP {exc.code}: {error_body}")
            if exc.code not in (429, 503):
                raise

    if not success_model:
        print("\nNone of the preferred Gemini models completed a generation request.", file=sys.stderr)
        sys.exit(3)

    print(f"\nWorking model: {success_model}")
except urllib.error.HTTPError as exc:
    error_body = exc.read().decode("utf-8", errors="replace")
    print(f"HTTP {exc.code}: {error_body}", file=sys.stderr)
    sys.exit(4)
except Exception as exc:
    print(f"Unexpected error: {exc}", file=sys.stderr)
    sys.exit(5)
