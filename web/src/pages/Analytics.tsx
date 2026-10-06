import { useQuery } from "@tanstack/react-query";
import { getAnalyticsSummary, getProjectAnalytics } from "../api/analytics";
import { ErrorBanner } from "../components/ErrorBanner";

export function AnalyticsPage() {
  const summary = useQuery({
    queryKey: ["analytics-summary"],
    queryFn: getAnalyticsSummary,
    refetchInterval: 15_000,
  });
  const projects = useQuery({
    queryKey: ["analytics-projects"],
    queryFn: getProjectAnalytics,
    refetchInterval: 15_000,
  });

  const cards = summary.data;

  return (
    <section>
      <div className="section-head">
        <h1>Analytics</h1>
        <button
          type="button"
          className="ghost"
          onClick={() => {
            void summary.refetch();
            void projects.refetch();
          }}
        >
          Refresh
        </button>
      </div>
      <p className="muted">Figures can lag behind the tracker. They are rebuilt from the event log, not from the live task tables.</p>
      <ErrorBanner error={summary.error ?? projects.error} />
      <div className="tile-grid">
        <Stat label="Total" value={cards?.total} />
        <Stat label="New" value={cards?.new} />
        <Stat label="In progress" value={cards?.inProgress} />
        <Stat label="Done" value={cards?.done} />
        <Stat label="Cancelled" value={cards?.cancelled} />
        <Stat
          label="Avg completion, days"
          value={cards?.averageCompletionDays == null ? "—" : cards.averageCompletionDays.toFixed(1)}
        />
      </div>
      <div className="card">
        <h2>Projects</h2>
        <table className="grid">
          <thead>
            <tr>
              <th>Project</th>
              <th>Total</th>
              <th>New</th>
              <th>In progress</th>
              <th>Done</th>
              <th>Cancelled</th>
            </tr>
          </thead>
          <tbody>
            {(projects.data ?? []).map((project) => (
              <tr key={project.projectId}>
                <td>{project.name}</td>
                <td>{project.total}</td>
                <td>{project.new}</td>
                <td>{project.inProgress}</td>
                <td>{project.done}</td>
                <td>{project.cancelled}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function Stat({ label, value }: { label: string; value: number | string | undefined }) {
  return (
    <div className="tile">
      <span className="muted">{label}</span>
      <strong>{value ?? "…"}</strong>
    </div>
  );
}
