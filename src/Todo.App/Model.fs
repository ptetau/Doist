module Todo.App.Model

open System
open Typed.Engine.Model

[<TitleType>]
type Title = Title of string

type Notes = Notes of string

type Priority =
    | Low
    | Normal
    | High

type TimeZoneId = TimeZoneId of string

type ExternalId = ExternalId of string

type User =
    { Subject: ExternalId
      DisplayName: Title
      TimeZone: TimeZoneId }

type TodoList = { Name: Title; Archived: bool }

type Todo =
    { List: Ref<TodoList>
      Title: Title
      Notes: Notes option
      Priority: Priority
      Due: DateOnly option
      Done: bool
      CompletedAt: DateTimeOffset option }
