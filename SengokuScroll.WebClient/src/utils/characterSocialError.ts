const messages: Record<string, string> = {
  SocialCooldown: "这对人物仍在社交冷却中：交谈每天一次，金钱赠礼每7天一次。",
  MarriageIneligible: "婚约条件不符：双方须年满18岁、无在世配偶且非近亲。",
  MarriageProposalCooldown: "同一人物的婚约提议需间隔30天。",
  MarriageProposalPending: "对方已有待处理婚约，请等待回应或到期。",
  MarriageProposalMissing: "没有这位人物提出的待处理婚约。",
  MarriageNotCoLocated: "双方须在同一地点确认婚约。",
  SocialPrisonerUnavailable: "被俘期间不能进行社交或婚约操作。",
  SocialRecipientTreasuryFull: "对方个人金库已满，赠礼未扣款。",
};
export function characterSocialError(message: string): string {
  return Object.entries(messages).find(([code]) => message.includes(code))?.[1] ?? message;
}
