export interface EnhancementRequest {
  field: string;
  value: string;
  context: Record<string, any>;
  style?:{
    tone?: "neutral"|"professional"|"friendly"|"concise"|"enthusiastic",
    formality?: "neutral"|"casual"|"formal",
    audience?:string,
    maxWords?: number,
    NumParagraphs?: number,
    language?: string,
    avoidPhrases?: string[],
    includeEEOBoilerplate?: boolean
  }
}

export interface EnhancementResponse {
  field: string;
  options: string[],
  meta:{
    model: string;
    promptTokens: number;
    completionTokens: number;
    totalTokens: number;
    finishReason: string;
  }
}
