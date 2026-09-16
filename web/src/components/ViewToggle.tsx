import { useCallback, useState } from "react";

export type ViewMode = "list" | "tiles";

export function useViewMode(storageKey: string, fallback: ViewMode = "tiles"): [ViewMode, (mode: ViewMode) => void] {
  const [mode, setMode] = useState<ViewMode>(() => {
    try {
      const saved = localStorage.getItem(storageKey);
      return saved === "list" || saved === "tiles" ? saved : fallback;
    } catch {
      return fallback;
    }
  });

  const update = useCallback(
    (next: ViewMode) => {
      setMode(next);
      try {
        localStorage.setItem(storageKey, next);
      } catch {
        /* ignore quota / private mode */
      }
    },
    [storageKey],
  );

  return [mode, update];
}

export function ViewToggle({ value, onChange }: { value: ViewMode; onChange: (mode: ViewMode) => void }) {
  return (
    <div className="view-toggle" role="group" aria-label="View mode">
      <button type="button" className={value === "list" ? "active" : ""} onClick={() => onChange("list")}>
        List
      </button>
      <button type="button" className={value === "tiles" ? "active" : ""} onClick={() => onChange("tiles")}>
        Tiles
      </button>
    </div>
  );
}
