export type TeamPerson = {
  id: string;
  teamId: string;
  personId: string;
};

export type SafePerson = {
  id: string;
  externalId: string;
  importId: number;
  username: string;
  password: string;
  firstName: string;
  lastName: string;
  displayName: string;
  email: string;
  phoneNumber: string;
  languageCodeId: number;
  vendLimitDaily: number;
  vendLimitTransaction: number;
  fullName: string;
  teamPersons: TeamPerson[];
};

export type UserSession = {
  id: string;
  personId: string;
  userSessionTypeCodeId: number;
  sessionStart: string;
  sessionEnd: string;
  apiToken: string;
  person: SafePerson;
};

export type LoginResponse = {
  isSystemUser: boolean;
  userSession: UserSession;
  typeUserSessionTypeCodeId: number;
  requiresPinChange: boolean;
};
