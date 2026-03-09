module HolidayScheduler.Strategies

open System
open System.Collections.Generic

// ── Domain Types ────────────────────────────────────────────────────────────

type EmployeeId = string
type DateOnly    = System.DateOnly

type HolidayRequest =
    { RequestId   : Guid
      EmployeeId  : EmployeeId
      StartDate   : DateOnly
      EndDate     : DateOnly
      Priority    : int          // 1 = highest
      SubmittedAt : DateTime }

type ScheduledHoliday =
    { Request   : HolidayRequest
      Reward    : float
      PlacedAt  : DateTime }

type SchedulerState =
    { Queue     : HolidayRequest list
      Schedule  : ScheduledHoliday list
      Blocked   : DateOnly Set    // dates already at capacity
      Capacity  : int             // max holidays per day }
    }

// ── Strategy Interface ───────────────────────────────────────────────────────

/// A strategy scores a request in the current state.  Higher score = better placement.
type Strategy = SchedulerState -> HolidayRequest -> float

// ── Pure Strategy Implementations ───────────────────────────────────────────

/// Greedy: always accept; reward = days in the request
let greedyStrategy : Strategy =
    fun _state req ->
        let days = (req.EndDate.DayNumber - req.StartDate.DayNumber) + 1
        float days

/// Load-balancing: prefer dates that are less loaded
let loadBalancingStrategy : Strategy =
    fun state req ->
        let days = (req.EndDate.DayNumber - req.StartDate.DayNumber) + 1
        let conflictDays =
            [ 0 .. days - 1 ]
            |> List.filter (fun d -> state.Blocked.Contains(req.StartDate.AddDays(d)))
            |> List.length
        float (days - conflictDays)

/// Priority-aware: higher-priority requests (lower number) score higher
let priorityAwareStrategy : Strategy =
    fun _state req ->
        let maxPriority = 10.0
        maxPriority - float req.Priority + 1.0

/// Temporal clustering: reward requests near existing scheduled holidays
let temporalClusteringStrategy : Strategy =
    fun state req ->
        let clusterBonus =
            state.Schedule
            |> List.sumBy (fun s ->
                let gap = abs (s.Request.StartDate.DayNumber - req.StartDate.DayNumber)
                if gap <= 7 then 2.0
                elif gap <= 30 then 1.0
                else 0.0)
        clusterBonus + 1.0

/// Learning: adjusts score based on historical acceptance rate (simple stub)
let learningStrategy (history : Map<EmployeeId, float>) : Strategy =
    fun _state req ->
        let rate = history |> Map.tryFind req.EmployeeId |> Option.defaultValue 0.5
        rate * 10.0

// ── Strategy Composition ────────────────────────────────────────────────────

/// Combine two strategies by averaging their scores
let combine (a : Strategy) (b : Strategy) : Strategy =
    fun state req -> (a state req + b state req) / 2.0

/// Weighted combination of strategies
let weighted (weights : (Strategy * float) list) : Strategy =
    fun state req ->
        let totalWeight = weights |> List.sumBy snd
        let weightedSum =
            weights |> List.sumBy (fun (s, w) -> s state req * w)
        if totalWeight > 0.0 then weightedSum / totalWeight else 0.0

/// Pipe: apply strategies sequentially; later strategies can only lower the score
let pipe (strategies : Strategy list) : Strategy =
    fun state req ->
        strategies
        |> List.map (fun s -> s state req)
        |> List.min

// ── Agent Genes ─────────────────────────────────────────────────────────────

type Genes =
    { GreedyWeight      : float
      LoadBalanceWeight : float
      PriorityWeight    : float
      ClusterWeight     : float
      MutationRate      : float }

let defaultGenes =
    { GreedyWeight      = 0.25
      LoadBalanceWeight = 0.25
      PriorityWeight    = 0.25
      ClusterWeight     = 0.25
      MutationRate      = 0.05 }

let private rng = Random()

let mutate (genes : Genes) : Genes =
    let nudge (v : float) =
        let delta = (rng.NextDouble() - 0.5) * genes.MutationRate * 2.0
        max 0.01 (v + delta)
    { genes with
        GreedyWeight      = nudge genes.GreedyWeight
        LoadBalanceWeight = nudge genes.LoadBalanceWeight
        PriorityWeight    = nudge genes.PriorityWeight
        ClusterWeight     = nudge genes.ClusterWeight }

let crossover (parent1 : Genes) (parent2 : Genes) : Genes =
    let pick a b = if rng.NextDouble() < 0.5 then a else b
    { GreedyWeight      = pick parent1.GreedyWeight      parent2.GreedyWeight
      LoadBalanceWeight = pick parent1.LoadBalanceWeight parent2.LoadBalanceWeight
      PriorityWeight    = pick parent1.PriorityWeight    parent2.PriorityWeight
      ClusterWeight     = pick parent1.ClusterWeight     parent2.ClusterWeight
      MutationRate      = pick parent1.MutationRate      parent2.MutationRate }

let genesStrategy (genes : Genes) (history : Map<EmployeeId, float>) : Strategy =
    weighted
        [ greedyStrategy,                        genes.GreedyWeight
          loadBalancingStrategy,                 genes.LoadBalanceWeight
          priorityAwareStrategy,                 genes.PriorityWeight
          temporalClusteringStrategy,            genes.ClusterWeight
          learningStrategy history,              0.1 ]

// ── Agent ────────────────────────────────────────────────────────────────────

type AgentMetrics =
    { TotalProcessed : int
      TotalReward    : float
      Generation     : int }

    member m.AverageReward =
        if m.TotalProcessed = 0 then 0.0
        else m.TotalReward / float m.TotalProcessed

type Agent =
    { Id           : Guid
      Genes        : Genes
      Metrics      : AgentMetrics
      History      : Map<EmployeeId, float> }

let createAgent genes generation =
    { Id      = Guid.NewGuid()
      Genes   = genes
      Metrics = { TotalProcessed = 0; TotalReward = 0.0; Generation = generation }
      History = Map.empty }

/// Process one request from the queue; returns updated agent and updated state.
/// Returns None if queue is empty.
let processOne (agent : Agent) (state : SchedulerState) : (Agent * SchedulerState) option =
    match state.Queue with
    | [] -> None
    | req :: rest ->
        let strategy = genesStrategy agent.Genes agent.History
        let reward   = strategy state req

        // Mark dates as used
        let days     = (req.EndDate.DayNumber - req.StartDate.DayNumber) + 1
        let newBlocked =
            [ 0 .. days - 1 ]
            |> List.fold (fun acc d ->
                let date = req.StartDate.AddDays(d)
                // Simple capacity: block only when we add the Nth booking (single-slot model)
                acc |> Set.add date) state.Blocked

        let scheduled =
            { Request = req; Reward = reward; PlacedAt = DateTime.UtcNow }

        let newSchedule = scheduled :: state.Schedule
        let newState    =
            { state with Queue = rest; Schedule = newSchedule; Blocked = newBlocked }

        // Update agent metrics and history
        let prevRate = agent.History |> Map.tryFind req.EmployeeId |> Option.defaultValue 0.5
        let newRate  = (prevRate * float agent.Metrics.TotalProcessed + 1.0) /
                       float (agent.Metrics.TotalProcessed + 1)
        let newAgent =
            { agent with
                Metrics =
                    { agent.Metrics with
                        TotalProcessed = agent.Metrics.TotalProcessed + 1
                        TotalReward    = agent.Metrics.TotalReward + reward }
                History = agent.History |> Map.add req.EmployeeId newRate }

        Some (newAgent, newState)

/// Drain the entire queue for a given agent
let processAll (agent : Agent) (state : SchedulerState) : Agent * SchedulerState =
    let rec loop a s =
        match processOne a s with
        | None        -> (a, s)
        | Some (a', s') -> loop a' s'
    loop agent state

// ── Multi-Generation Evolution ───────────────────────────────────────────────

type Population = Agent list

let createPopulation size =
    List.init size (fun i -> createAgent (mutate defaultGenes) i)

/// Select top-N agents by average reward
let selectElite (n : int) (pop : Population) : Population =
    pop
    |> List.sortByDescending (fun a -> a.Metrics.AverageReward)
    |> List.truncate n

/// Spawn a new generation from elite parents
let spawnGeneration (generation : int) (elites : Population) (targetSize : int) : Population =
    let eliteCount = List.length elites
    if eliteCount = 0 then createPopulation targetSize
    else
        List.init targetSize (fun i ->
            let parent1 = elites.[i % eliteCount]
            let parent2 = elites.[(i + 1) % eliteCount]
            let childGenes = crossover parent1.Genes parent2.Genes |> mutate
            // Inherit average-reward history influence via mutation rate
            createAgent childGenes generation)

/// Run one full generation: process all queued requests, evolve, return new population
let evolveGeneration
    (population   : Population)
    (initialState : SchedulerState)
    (eliteCount   : int)
    (targetSize   : int)
    (generation   : int)
    : Population * SchedulerState =

    // Each agent gets its own copy of the initial state and processes independently
    let results =
        population
        |> List.map (fun agent -> processAll agent initialState)

    // Pick best final state by total reward in schedule
    let bestState =
        results
        |> List.maxBy (fun (_, s) ->
            s.Schedule |> List.sumBy (fun sh -> sh.Reward))
        |> snd

    let evolvedAgents = results |> List.map fst
    let elites        = selectElite eliteCount evolvedAgents
    let newPop        = spawnGeneration generation elites targetSize

    (newPop, bestState)
