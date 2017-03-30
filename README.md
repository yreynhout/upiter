# Introduction

Upiter is an online messaging platform written in F# using patterns such as CQRS & EventSourcing. It leverages ideas put forward in https://github.com/thinkbeforecoding/FsUno.Prod and https://github.com/thinkbeforecoding/FsUno when it comes to command handling. On one hand it's a playground for ideas and implementations, on the other hand it tries to be a serious attempt and doesn't shy away from overengineering.

# Core Concepts

- Platform Visitors: People that visit the platform.
- Platform Members: People who have signed up for the platform and are thus a member of the platform.
- Platform Guests: People who haven't yet signed up, the visitors or passers-by that still need to be _converted_, or the platform members who, for all intents, wish to stay anonymous during a particular interaction with the platform. Who knows, right?
- Platform Administrators: People with great power within the scope of a particular tenant.

- Group Members: People that became member of a group either thru invitation or by request.
- Group Guests: Any platform guest or platform member that does not have a membership with the group.
- Group Moderators: People who moderate the content that is posted within a group.
