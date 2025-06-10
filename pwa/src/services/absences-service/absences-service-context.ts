import { createContext } from '@lit/context';
import type { AbsencesService } from './absences-service';
export type { AbsencesService } from './absences-service';

export const absencesServiceContext = createContext<AbsencesService>('absences-service');