import { useQuery } from "@tanstack/react-query";
import { getDirectory } from "../api/users";
import type { UserDto } from "../types";

export function useDirectoryMap() {
  const query = useQuery({ queryKey: ["directory"], queryFn: getDirectory });
  const map = new Map<string, UserDto>((query.data ?? []).map((user) => [user.id, user]));
  return { ...query, map };
}

export function userLabel(map: Map<string, UserDto>, id: string): string {
  const user = map.get(id);
  return user ? `${user.username} (${user.email})` : id.slice(0, 8);
}
