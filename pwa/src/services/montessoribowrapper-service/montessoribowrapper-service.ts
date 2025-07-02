import { httpClient } from '../http-client';

export class MontessoriBoWrapperService {

    public async getHomeData(): Promise<any> {
        try {
            const response = await httpClient.get<any>(
                `/api/proxy/home`
            );
            return response.data;
        } catch (error) {
            console.error('Error gethomeData:', error);
            throw error; // Re-throw the error for further handling if needed
        }
    }

    public async getPage(label: string): Promise<string> {
        try {
            const response = await httpClient.get<string>(
                `/api/proxy/` + label
            );
            return response.data;
        } catch (error) {
            console.error('Error getPage:' + label, error);
            throw error; // Re-throw the error for further handling if needed
        }
    }
}