import { create } from 'zustand';
import type { AppRole } from '../shared/types/layout.types';

interface AppState {
  currentRole: AppRole;
  setCurrentRole: (role: AppRole) => void;
}

export const useAppStore = create<AppState>((set) => ({
  currentRole: 'department_head',
  setCurrentRole: (role) => set({ currentRole: role }),
}));
