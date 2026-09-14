import { spawn } from "node:child_process";
import { existsSync, lstatSync, symlinkSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const webDir = dirname(fileURLToPath(new URL(".", import.meta.url)));
let cwd = webDir;

if (webDir.includes("#")) {
  const dest = join(process.env.LOCALAPPDATA || process.env.TEMP || webDir, "taskflow-web");
  let ready = false;
  try {
    ready = existsSync(dest) && lstatSync(dest).isDirectory();
  } catch {
    ready = false;
  }
  if (!ready) {
    symlinkSync(webDir, dest, "junction");
  }
  cwd = dest;
  console.log(`Vite root has '#'; running from junction ${dest}`);
}

const child = spawn("npx", ["vite"], { cwd, stdio: "inherit", shell: true });
child.on("exit", (code) => process.exit(code ?? 0));
