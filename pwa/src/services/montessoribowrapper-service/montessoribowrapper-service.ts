import { httpClient } from '../http-client';

export class MontessoriBoWrapperService {

    public async getHomeData(): Promise<any> {
        try {
            const response = await httpClient.get<any>(
                `/api/home`
            );
            return response.data;
        } catch (error) {
            console.error('Error gethomeData:', error);
            throw error; // Re-throw the error for further handling if needed
        }
    }

    public async getLicenciasPage(): Promise<string> {
        try {
            const response = await httpClient.get<string>(
                `/api/licencias`
            );
            return response.data;
        } catch (error) {
            console.error('Error getLicenciasPage:', error);
            throw error; // Re-throw the error for further handling if needed
        }
    }
}