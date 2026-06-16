#!/usr/bin/env python3

import argparse
import base64
import json
from pathlib import Path


def extract_contents(args: argparse.Namespace) -> int:
    response_path = Path(args.response_path)
    manifest_path = Path(args.manifest_path)
    sha_path = Path(args.sha_path)

    payload = json.loads(response_path.read_text(encoding="utf-8"))
    encoded_content = str(payload.get("content", "")).replace("\n", "")

    if encoded_content:
        manifest_path.write_text(
            base64.b64decode(encoded_content).decode("utf-8"),
            encoding="utf-8",
        )
    else:
        manifest_path.write_text("[]\n", encoding="utf-8")

    sha_path.write_text(str(payload.get("sha", "")), encoding="utf-8")
    return 0


def write_put_payload(args: argparse.Namespace) -> int:
    manifest_path = Path(args.manifest_path)
    output_path = Path(args.output_path)
    content = base64.b64encode(manifest_path.read_bytes()).decode("ascii")

    payload: dict[str, str] = {
        "message": f"Update {args.plugin_name} {args.version}",
        "content": content,
        "branch": args.branch,
    }

    if args.sha:
        payload["sha"] = args.sha

    output_path.write_text(json.dumps(payload), encoding="utf-8")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Helpers for reading and writing GitHub manifest repository API payloads."
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    extract_parser = subparsers.add_parser(
        "extract-contents",
        help="Decode a GitHub contents API response into manifest and sha files.",
    )
    extract_parser.add_argument("--response-path", required=True)
    extract_parser.add_argument("--manifest-path", required=True)
    extract_parser.add_argument("--sha-path", required=True)
    extract_parser.set_defaults(handler=extract_contents)

    payload_parser = subparsers.add_parser(
        "write-put-payload",
        help="Build a GitHub contents API PUT payload from a local manifest file.",
    )
    payload_parser.add_argument("--manifest-path", required=True)
    payload_parser.add_argument("--output-path", required=True)
    payload_parser.add_argument("--plugin-name", required=True)
    payload_parser.add_argument("--version", required=True)
    payload_parser.add_argument("--branch", required=True)
    payload_parser.add_argument("--sha", default="")
    payload_parser.set_defaults(handler=write_put_payload)

    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()
    return args.handler(args)


if __name__ == "__main__":
    raise SystemExit(main())
