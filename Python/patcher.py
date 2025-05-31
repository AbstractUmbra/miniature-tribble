import argparse
import datetime
import pathlib
import tempfile
from typing import TypedDict

import orjson
import platformdirs

APP_DATA = platformdirs.user_data_path().parent
ROAMING = APP_DATA / "Roaming"
XIV_LAUNCHER = ROAMING / "XIVLauncher"
CONFIG_FILE = XIV_LAUNCHER / "dalamudConfig.json"


class DalamudConfigFile(TypedDict, total=False):
    betaKind: str
    betaKey: str


class ProgramNamespace(argparse.Namespace):
    beta_key: str
    beta_kind: str
    dry_run: bool
    no_backup: bool


parser = argparse.ArgumentParser(
    description="Small CLI tool to update your dalamud config with beta key information."
)
parser.add_argument(
    "-bke",
    "--beta-key",
    default="",
    type=str,
    help="The beta key to use, defaults to resetting to empty value.",
    dest="beta_key",
)
parser.add_argument(
    "-bki",
    "--beta-kind",
    default="release",
    type=str,
    help="The beta kind we're opting into. Defaults to stable.",
    dest="beta_kind",
)
parser.add_argument(
    "-d",
    "--dry-run",
    type=bool,
    action="store_true",
    help="Whether or not to edit the destination file, or just print the contents.",
    dest="dry_run",
)
parser.add_argument(
    "-n",
    "--no-backup",
    type=bool,
    action="store_true",
    help="Whether to opt out of backing up your config file before edits.",
    dest="no_backup",
)

args: ProgramNamespace = parser.parse_args(namespace=ProgramNamespace())

if not CONFIG_FILE.exists(follow_symlinks=True):
    raise RuntimeError(
        f"The config file doesn't exist at the path {CONFIG_FILE}. Exiting."
    )


def atomic_overwrite(contents: DalamudConfigFile) -> None:
    with tempfile.NamedTemporaryFile() as tmp_f:
        tmp_path = pathlib.Path(tmp_f.name)
        tmp_f.write(orjson.dumps(contents))

        tmp_path.replace(CONFIG_FILE)


def backup_file() -> pathlib.Path:
    import shutil

    now = datetime.datetime.now()
    stringed = now.strftime("%d-%m-%Y--%H-%M")
    backup = CONFIG_FILE.with_stem(f"dalamudConfig.backup-{stringed}")
    shutil.copy(CONFIG_FILE, backup)

    return backup


def update_contents(
    contents: DalamudConfigFile, *, beta_kind: str, beta_key: str
) -> DalamudConfigFile:
    contents["betaKind"] = beta_kind
    contents["betaKey"] = beta_key

    return contents


file_contents = CONFIG_FILE.read_text()
resolved_contents: DalamudConfigFile = orjson.loads(file_contents)
updated = update_contents(
    resolved_contents, beta_key=args.beta_key, beta_kind=args.beta_kind
)

if args.dry_run:
    print(updated)
    exit(0)

if not args.no_backup:
    backup_file()

atomic_overwrite(updated)
