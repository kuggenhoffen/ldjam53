using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WordList
{
    static string wordList = 
      @"time
        way
        year
        work
        government
        day
        man
        world
        life
        part
        house
        course
        case
        system
        place
        end
        group
        company
        party
        information
        school
        fact
        money
        point
        example
        state
        business
        night
        area
        water
        thing
        family
        head
        hand
        order
        john
        side
        home
        development
        week
        power
        country
        council
        use
        service
        room
        market
        problem
        court
        lot
        a
        war
        police
        interest
        car
        law
        road
        form
        face
        education
        policy
        research
        sort
        office
        body
        person
        health
        mother
        question
        period
        name
        book
        level
        child
        control
        society
        minister
        view
        door
        line
        community
        south
        city
        god
        father
        centre
        effect
        staff
        position
        kind
        job
        woman
        action
        management
        act
        process
        north
        age
        evidence
        idea";

    static public string getWord()
    {
        string[] words = wordList.Split('\n');
        return words[Random.Range(0, words.Length)].ToUpper().Trim();
    }

}
