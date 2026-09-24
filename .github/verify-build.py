#!/usr/bin/env python3
"""Sanity-check a built ValheimPlus.dll from this fork before it is released.

Exists because of a real incident. A guard grepped the built assembly for an IL string literal in
ASCII, always failed, the failure skipped an install step, and a build shipped in which the new
setting was read correctly but never applied to anything.

The lesson is about metadata heaps. A .NET assembly keeps these in different places:

  identifiers (type, method and field names)   #Strings heap, UTF-8
  custom attribute arguments                   the attribute blob, UTF-8 length-prefixed
  IL string literals                           #US heap, UTF-16

So each thing below is looked for in the encoding it is actually stored in. Checking a config key
is not enough on its own either: the key existed in the broken build. The prefab name it compares
against is the part that has to be right.

    python3 .github/verify-build.py ValheimPlus/bin/Release/ValheimPlus.dll
"""
import sys

# The upstream version this fork is based on. Deliberately NOT bumped: Server.enforceMod compares
# version strings, so keeping it means clients on official ValheimPlus can still connect. Our own
# release numbering lives in the git tag instead.
BASE_VERSION = "0.10.2.0"

# Config entries -> bound by name, so they reach the file as identifiers (UTF-8).
CONFIG_KEYS = ("softTissue", "extraPlayerInventoryRows")

# Prefab names compared in a switch -> IL string literals (UTF-16). "Softtissue" has a lowercase
# t and lives under Items/consumables; "SoftTissue" is the material/texture folder and is the
# wrong string. Getting this back to front is exactly what shipped broken once.
PREFAB_LITERALS = ("Softtissue",)


def main() -> int:
    if len(sys.argv) != 2:
        print(__doc__)
        return 2
    dll = sys.argv[1]

    try:
        blob = open(dll, "rb").read()
    except OSError as e:
        print(f"error: cannot read {dll}: {e}")
        return 1

    problems = []

    # Reaches the file through [BepInPlugin(...)], whose arguments are UTF-8 in the attribute blob.
    if BASE_VERSION.encode() not in blob:
        problems.append(
            f"the base version {BASE_VERSION} is not present. This fork must keep upstream's "
            f"version string so enforceMod still matches official ValheimPlus."
        )

    for key in CONFIG_KEYS:
        if key.encode() not in blob:
            problems.append(f"config entry {key} is missing (checked UTF-8, #Strings)")

    for literal in PREFAB_LITERALS:
        if literal.encode("utf-16-le") not in blob:
            problems.append(
                f"prefab literal {literal!r} is missing (checked UTF-16, #US). The setting would "
                f"read fine and match nothing."
            )

    if problems:
        print(f"{dll} failed verification:")
        for p in problems:
            print(f"  - {p}")
        return 1

    print(
        f"verified {dll}: base version {BASE_VERSION}, "
        f"{len(CONFIG_KEYS)} config entries, {len(PREFAB_LITERALS)} prefab literals"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
