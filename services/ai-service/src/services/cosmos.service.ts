import { CosmosClient, Container } from "@azure/cosmos";

import {JobGenResponse} from "../schemas/job-generate.js";
import {z} from "zod";
type JobGenResponseT = z.infer<typeof JobGenResponse>;
export class CosmosService{
    private client: CosmosClient;
    constructor() {
        this.client = new CosmosClient({
            endpoint: process.env.COSMOS_ENDPOINT!,
            key: process.env.COSMOS_KEY!,
            connectionPolicy: { enableEndpointDiscovery: false }
        });
    }

    async saveDraft(companyId:string, result: JobGenResponseT){
        const container = this.getContainer("drafts");
        await container.items.upsert({...result, companyId});
    }

    private getContainer(container:string): Container {
        return this.client.database(process.env.COSMOS_DB!).container(container)
    }
}

