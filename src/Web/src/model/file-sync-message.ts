export enum FileSyncMessageType {
    UpdateTime = 0,
    ClientId = 1,
}

export interface FileUpdatedMessage {
    messageType: FileSyncMessageType.UpdateTime;
    time: string | Date;
    senderClientId: string;
}

export interface ClientIdMessage {
    messageType: FileSyncMessageType.ClientId;
    clientId: string;
}

export type FileSyncMessage = FileUpdatedMessage | ClientIdMessage;
