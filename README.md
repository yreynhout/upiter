# Introduction

Upiter is an online messaging platform written in F# using patterns such as CQRS & EventSourcing. It leverages ideas put forward in https://github.com/thinkbeforecoding/FsUno.Prod and https://github.com/thinkbeforecoding/FsUno when it comes to command handling. On one hand it's a playground for ideas and implementations, on the other hand it tries to be a serious attempt and doesn't shy away from overengineering.

# Core Concepts

## About Tenancy

Upiter as a platform is designed to be multi tenant. The current idea is to hand out one or more subdomains to a tenant. Each tenant is also uniquely identified by an `integer`. Tenant sign-up is considered - for the time being - to be a slow, manual process outside the scope of the platform itself. The platform just needs to be aware of the list of `(tenantid: int32, subdomains: string[])` pairs.

## About People

There are a lot of people interacting with the platform. In order to make reasoning about their capabilities easier, we've classified them into the following roles. Bear in mind that the same person could assume more than one role depending on the context and that he might transition from one role to another over the course of time.

- Platform Visitors: Simply put, these are the people that visit the platform.
- Platform Members: People who have signed up for the platform and are thus considered a _member_ of the platform.
- Platform Guests: People who haven't yet signed up, the visitors or passers-by that still need to be _converted_, or platform members who, for all intents, wish to stay anonymous during a particular interaction with the platform.
- Platform Administrators: People with great power within the scope of a particular tenant.

- Group Members: People that became member of a group either thru invitation or by request.
- Group Guests: Any platform guest or platform member that does not have a membership with the group.
- Group Moderators: People who moderate the content that is posted within a group.
