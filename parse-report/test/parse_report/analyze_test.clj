(ns parse-report.analyze-test
   (:require [clojure.test :refer :all]
             [parse-report.analyze :refer :all])
  (:import [parse_report.analyze GateHits]))

(deftest should-split-a-line
  (is (= (list "1" "2" "3") (split-line  "1,2,3"        #",")))
  (is (= (list "1" "2" "3") (split-line  " 1 , 2,3   "  #","))))


(deftest should-parse-distance
    (are [distance line] (= distance (parse-distance line))
         1 "1 day"
         3 "3 days"
         -1 "impassable"))

(deftest should-extract-from-brackets
    (are [exp-word exp-id line] (= [exp-word exp-id] (parse-word-id line))
        "to Forest"             "aa11"  "to Forest [aa11]"
        "Demons and Minions"    "ca7"   "Demons and Minions [ca7]."))

(deftest should-guess-type-from-name
    (is (= "forest" (guess-type-from-name "Forest")))
    (is (= nil (guess-type-from-name "Feynesse"))))

(deftest should-get-route-info
    (are [exp-route exp-loc splitline] (= [exp-route exp-loc] (get-route-info splitline))
        (make-route "aa11" "North" 7 false) (make-location "Forest" "aa11" "forest")
            ["North" "to Forest [aa11]" "7 days"]
        (make-route "k32" "South" 1 false) (make-location "Clearhaven" "k32" "city")
            ["South" "city" "to Clearhaven [k32]" "Albea" "1 day"]
        (make-route "nz32" "Underground" 1 true) (make-location "Hades" "nz32" "hades")
            ["Underground" "to Hades [nz32]" "Hades" "hidden" "1 day"]
        (make-route "b917" "West" 5 true) (make-location "Chamber" "b917" "chamber")
            ["West" "to Chamber [b917]" "hidden" "5 days"]
        (make-route "bj45" "Out" 1 false) (make-location "Mountain" "bj45" "mountain")
            ["Out" "to Mountain [bj45]" "1 day"]
        (make-route "bh45" "East" 7 false) (make-location "Tamar Valley" "bh45" "plain")
            ["East" "plain" "to Tamar Valley [bh45]" "7 days"]
		(make-route "dw76" "Id" 1 false) (make-location "Mountain" "dw76" "mountain")
			["To Mountain [dw76]" "Ugul" "1 day"]
		(make-route "cn50" "South" 10 false) (make-location "Mt. Olympus" "cn50" "mountain")
		   ["South" "mountain" "to Mt. Olympus [cn50]" "10 days"]
	    (make-route "hk70" "Up" -1 false) (make-location "Cloud" "hk70" "cloud")
		    ["Up" "cloud" "to Cloud [hk70]" "Cloudlands" "impassable"]
		(make-route "sg79" "East" 7 true) (make-location "Hades" "sg79" "underground")
		    ["East" "underground" "to Hades [sg79]" "hidden" "7 days"]
			))


(deftest should-get-inner-loc-info
    (is (= [(make-location "Rocky Hill" "k123" "rocky hill")
            (make-route "k123" "In" 1 false)]
        (get-inner-loc-info ["Rocky Hill [k123]" "rocky hill" "1 day"])))
    (is (= [(make-location "Island" "z066" "island")
            (make-route "z066" "In" 1 true)]
        (get-inner-loc-info ["Island [z066]" "island" "hidden" "1 day"])))
    (is (= [(make-location "Old battlefield" "c897" "battlefield")
            (make-route "c897" "In" 1 false)]
        (get-inner-loc-info ["Old battlefield [c897]" "battlefield" "1 day" "owner:"])))
    (is (= [(make-location "Cave" "n130" "cave")
            (make-route "n130" "In" 1 true)]
        (get-inner-loc-info ["Cave [n130]" "cave" "hidden" "1 day" "owner:"])))
    (is (= [(make-location "Cathedral of knowledge" "5046" "tower-in-progress")
            (make-route "5046" "In" 0 false)]
        (get-inner-loc-info ["Cathedral of knowledge [5046]" "tower-in-progress" "35% completed" "owner:"])))
    ; (is (= nil
        ; (get-inner-loc-info ["contact imperialeflotte@gmx.de\"" "owner:"])))
    (is (= [(make-location "Sewer" "s613" "sewer")
            (make-route "s613" "In" 0 true)]
        (get-inner-loc-info ["Sewer [s613]" "sewer" "hidden" "owner:"]))))

(deftest should-merge-locations
	(are [merged existing new] (= merged (merge-locations existing new))
		; straightforward merge - both are the same, just need to merge visits
		(make-location "Rocky Hill" "k123" "rocky hill" #{ (make-route "k123" "In" 1 false) } #{ [3 "pk2"] [2 "ca3"]} )
		(make-location "Rocky Hill" "k123" "rocky hill" #{ (make-route "k123" "In" 1 false) } #{ [2 "ca3"]} )
		(make-location "Rocky Hill" "k123" "rocky hill" #{ (make-route "k123" "In" 1 false) } #{ [3 "pk2"]} )
		; keep the most recent only - don't merge unhidden routes, as they should be in the most recent location
		; it's buildings that have been removed -> d
		;(make-location "Rocky Hill" "k123" "rocky hill" #{ (make-route "k123" "In" 1 false) } #{ [3 "pk2"] [2 "ca3"]} )
		;(make-location "Rocky Hill" "k123" "rocky hill" #{ (make-route "k123" "In" 1 false) (make-route "1234" "In" 1 false) } #{ [2 "ca3"]} )
		;(make-location "Rocky Hill" "k123" "rocky hill" #{ (make-route "k123" "In" 1 false) } #{ [3 "pk2"]} )
		;similar as above, but check that hidden routes are still merged even if not the last visited
		(make-location "Rocky Hill" "k123" "rocky hill" #{ (make-route "k123" "In" 1 false) (make-route "1234" "In" 1 true) } #{ [3 "pk2"] [2 "ca3"]} )
		(make-location "Rocky Hill" "k123" "rocky hill" #{ (make-route "k123" "In" 1 false) (make-route "1234" "In" 1 true) } #{ [2 "ca3"]} )
		(make-location "Rocky Hill" "k123" "rocky hill" #{ (make-route "k123" "In" 1 false) } #{ [3 "pk2"]} )

	))

(deftest should-detect-city-type
    (is (city? "port city"))
    (is (city? "city"))
    (is (not (city? "forest"))))

(deftest should-add-route-to-loc
    (is (= (make-location "Plain" "aa11" "plain" #{ (make-route "pq00" "North" 4 false) })
        (add-route (make-route "pq00" "North" 4 false) (make-location "Plain" "aa11" "plain")))))

(deftest should-add-location
    (let [test-loc (make-location "Plain" "aa11" "plain" )
          test-loc-with-route (make-location "Plain" "aa11" "plain" #{ (make-route  "pq00" "North" 4 false) })]
    (is (= {(:id test-loc) test-loc} (add-loc {} test-loc)))
    (is (= {(:id test-loc) test-loc} (add-loc (add-loc {} test-loc) test-loc)))
    (is (= {(:id test-loc) test-loc-with-route} (add-loc { "aa11" test-loc-with-route} test-loc)))))

(deftest should-get-rumored-city-info
    (let [[actual-city actual-province] (get-rumored-city-info "   Barney [k43], in Forest [bj49]")]
    (is (= (make-location "Barney" "k43" "city") actual-city))
    (is    (= (make-location "Forest" "bj49" "forest" #{ (make-route "k43" "In" 1 false) }) actual-province))))

(deftest should-get-province-control-info
    (are [expected line] (= expected (get-province-control-info line))
        ["6452" "bn60"] "Province controlled by Castle Anthrax [6452], castle, in Mountain [bn60]"))

(deftest should-get-city-skills
    (is (= (list 600 610 630 680) (get-skills "Shipcraft [600], Combat [610], Stealth [630], Construction [680],")))
    (is (= (list 670 680 800) (get-skills "Persuasion [670], Construction [680], Magic [800], Artifact"))))

(deftest should-remove-html-markup
    (is (= "Swamp [bp66], swamp, in Albea, wilderness"
        (remove-html "<name=\"Swamp [bp66]\"><font size=+1><b>Swamp [bp66]</b>, swamp, in Albea, wilderness</font>"))))

(deftest shoud-remove-and-return-day
    (are [expected line] (= expected (remove-day line))
         ["Jinx [3633] stacks beneath us." 3] "3: Jinx [3633] stacks beneath us."
         ["   Out, to Forest [bz65], 1 day" 30] "30:    Out, to Forest [bz65], 1 day"
         ["" 24] "24:"
         ["   North, to Ocean [bk61], Great Sea, impassable" nil] "   North, to Ocean [bk61], Great Sea, impassable"))

(deftest should-get-civ-info
    (is (= 0 (get-civ-info "Albea, wilderness")))
    (is (= 1 (get-civ-info "Albea, civ-1")))
    (is (= nil (get-civ-info "Forest [bh44], hidden"))))

(deftest should-get-loc-info
    (is (= (assoc (make-location "Swamp" "bp66" "swamp") :region "Albea" :civ 0 :visits #{[12 "ag3"]})
        (get-loc-info "<name=\"Swamp [bp66]\"><font size=+1><b>Swamp [bp66]</b>, swamp, in Albea, wilderness</font>" 12 "ag3")))
    (is (= (assoc (make-location "Plain" "bh45" "plain") :region "Albea" :civ 1 :visits #{[12 "ag3"]})
        (get-loc-info "<name=\"Plain [bh45]\"><font size=+1><b>Plain [bh45]</b>, plain, in Albea, civ-1</font>" 12 "ag3")))
    (is (= (assoc(make-location "Clearhaven" "k32" "port city") :region "province Forest [bq71]" :visits #{[12 "ag3"]})
        (get-loc-info "<name=\"Clearhaven [k32]\"><font size=+1><b>Clearhaven [k32]</b>, port city, in province Forest [bq71], safe haven</font>" 12 "ag3")))
    (is (= (assoc(make-location "Whiteoak" "s94" "city") :region "province Swamp [bk46]" :visits #{[12 "ag3"]})
        (get-loc-info "<name=\"Whiteoak [s94]\"><font size=+1><b>Whiteoak [s94]</b>, city, in province Swamp [bk46]</font>" 12 "ag3")))
    (is (= (assoc(make-location "Sewer" "s613" "sewer") :region "Forest [bh44]" :hidden true :visits #{[12 "ag3"]})
        (get-loc-info "<name=\"Sewer [s613]\"><font size=+1><b>Sewer [s613]</b>, sewer, in Forest [bh44], hidden</font>" 12 "ag3")))
    (is (= (assoc(make-location "Tunnel" "td97" "tunnel") :region "Undercity" :visits #{[12 "ag3"]})
        (get-loc-info "<name=\"Tunnel [td97]\"><font size=+1><b>Tunnel [td97]</b>, tunnel, in Undercity</font>" 12 "ag3")))
    (is (= (assoc(make-location "Ocean" "bd59" "ocean")  :region "Great Sea" :visits #{[12 "ag3"]})
        (get-loc-info "<name=\"Ocean [bd59]\"><font size=+1><b>Ocean [bd59]</b>, ocean, in Great Sea</font>" 12 "ag3")))
    (is (= (assoc(make-location "Ravencroft" "h67" "city")  :region "province Forest [bz65]" :visits #{[12 "ag3"]})
        (get-loc-info "30: Ravencroft [h67], city, in province Forest [bz65]" 12 "ag3")))
    (is (= (assoc(make-location "Feynesse Mountains" "bj45" "mountain") :region "Albea" :civ 4 :visits #{[12 "ag3"]})
        (get-loc-info "<name=\"Feynesse Mountains [bj45]\"><font size=+1><b>Feynesse Mountains [bj45]</b>, mountain, in Albea, civ-4</font>" 12 "ag3")))
    (is (= nil
        (get-loc-info "   Location:       Pasture [k808], in province Plain [bc61], in Carisos" 12 "ag3")))
    )

(deftest should-match-inner-loc
    (let [test-loc (make-location "Plain" "aa11" "plain" #{})
          expected-route (make-route "j835" "In" 1 false)
          expected-loc (make-location "Rocky hill" "j835" "rocky hill" #{} #{[1 "ak3"]})
          expected-route2 (make-route "k32" "In" 1 false)
          expected-loc2 (make-location "Clearhaven" "k32" "port city" #{} #{[2 "ak2"] })
          expected {:loc-in-progress (add-route expected-route test-loc) :locations { "j835" expected-loc }}
          actual (match-inner-loc {:loc-in-progress test-loc :locations {} :line "2:    Rocky hill [j835], rocky hill, 1 day" :turn 1 :faction "ak3"})
          expected2 {:loc-in-progress (add-route expected-route2 test-loc) :locations { "k32" expected-loc2 }}
          actual2 (match-inner-loc {:loc-in-progress test-loc :locations {} :line "   Clearhaven [k32], port city, safe haven, 1 day" :turn 2 :faction "ak2"})]
    (are [actual expected] (and (= (:loc-in-progress actual) (:loc-in-progress expected))
                                (= (:locations actual) (:locations expected)))
        actual expected
        actual2 expected2 )))

(deftest should-match-loc-header
    (let [test-loc-in-progress (make-location "Plain" "aa11" "plain")
          test-city "<name=\"Clearhaven [k32]\"><font size=+1><b>Clearhaven [k32]</b>, port city, in province Forest [bq71], safe haven</font>"
          test-province "<name=\"Swamp [bp66]\"><font size=+1><b>Swamp [bp66]</b>, swamp, in Albea, wilderness</font> "
          expected-province (assoc(make-location "Swamp" "bp66" "swamp") :region "Albea" :civ 0 :visits #{ [3 "ug3"] })]
    (are [expected-loc-in-progress expected-locations actual]
        (and (= (:loc-in-progress actual) expected-loc-in-progress)
             (= (:locations actual) expected-locations))
    nil nil (match-loc-header {:line "   Location:       Pasture [k808], in province Plain [bc61], in Carisos" :turn 3 :faction "ug3"})
    test-loc-in-progress nil (match-loc-header {:loc-in-progress test-loc-in-progress :line "Not a location" })
    expected-province nil (match-loc-header {:line test-province :turn 3 :faction "ug3"})
    expected-province {(:id test-loc-in-progress) test-loc-in-progress} (match-loc-header {:loc-in-progress test-loc-in-progress :locations {} :line test-province :turn 3 :faction "ug3"})
    (assoc (make-location "Clearhaven" "k32" "port city") :region "province Forest [bq71]" :visits #{ [23 "er2"] }) nil
        (match-loc-header {:line test-city :turn 23 :faction "er2"}) )))

(deftest should-determine-indent
    (are    [indent line] (= indent (indent-of line))
            0       "Desert [bk59], desert, in Albea, civ-1"
            3       "   North, to Ocean [bj59], Great Sea, impassable"
            1       " Murk [7689], with three riding horses, arrived from Desert [bk59]."
            3       " * Murk [7689], with three riding horses"
            6       " *    Dreg [6338], with three riding horses"))

(deftest should-match-turn-and-faction
    (are [exp-turn exp-faction line]
        (let [{ :keys [turn faction]} (match-turn-and-faction {:line line})]
        (and (= exp-turn turn) (= exp-faction faction)))
        9   nil                          "Olympia G4 turn 9"
        nil "ca7" "Report for Demons and Minions [ca7]."
        nil "ca7" "Initial Position Report for Demons and Minions [ca7]."))

(:turn (match-turn-and-faction {:line "Olympia G3 turn 9"}))

(deftest should-add-new-noble
    (are [expected new-noble nobles] (= expected (add-new-noble new-noble nobles))
        { "1234" (make-noble "Foo" "1234" "vc4") } (make-noble "Foo" "1234" "vc4") {}
        { "1234" (make-noble "Baa" "1234" "vc4") } (make-noble "Foo" "1234" "vc4") {"1234" (make-noble "Baa" "1234" "vc4")}
        ))

(deftest should-add-new-noble-location
    (is (= (make-noble "Foo" "1234" "vc4" [[1 "ca34"]])
           (add-new-noble-location (make-noble "Foo" "1234" "vc4" []) 1 "ca34"))))

(deftest should-get-noble-location-info
    (is (= "bx66" (get-noble-location-info "   Location:       Plain [bx66], in Albea")))
    (is (= "p19" (get-noble-location-info "   Location:       Millerscreek [p19], in province Desert [bq67], in Albea")))
    (is (= "b750" (get-noble-location-info "   Location:       Graveyard [b750], in province Forest [br63], in Albea")))
    (is (= "7001" (get-noble-location-info "   Location:       Z [7001], in Millerscreek [p19], in province")))
    (is (= "7001" (get-noble-location-info "   Location:       A\nB [7001], in Millerscreek [p19], in province")))
    (is (= nil (get-noble-location-info "12: Received 4 gold from Garrison [2851]."))))

 (deftest should-match-noble-header
    (are [exp-noble-in-progress
          exp-nobles
          noble-in-progress-in
          nobles-in
          turn
          faction
          line]
        (let [{ :keys [noble-in-progress nobles]  }
                (match-garrisons-then-noble-header  { :noble-in-progress noble-in-progress-in
                                       :nobles nobles-in
                                       :turn turn
                                       :faction faction
                                       :line line })]
        (= [exp-noble-in-progress exp-nobles] [noble-in-progress nobles]))
        (make-noble "Murk" "7689" "faction") {} nil {} 1 "faction" "<name=\"Murk [7689]\"><font size=+1><b>Murk [7689]</b></font>"
        (make-noble "Murk" "a689" "faction") {} nil {} 1 "faction" "<name=\"Murk [a689]\"><font size=+1><b>Murk [a689]</b></font>"
        (make-noble "Bockor I" "9367" "faction") {} nil {} 1 "faction" "<name=\"Bockor I [9367]\"><font size=+1><b>Bockor I [9367]</b></font>"
        (make-noble "Flurk" "1111" "faction") { "7689" (make-noble "Murk" "7689" "faction")} (make-noble "Murk" "7689" "faction") {} 1 "faction" "<name=\"Flurk [1111]\"><font size=+1><b>Flurk [1111]</b></font>"
        (make-noble "Paolo Giovanni" "1577" "faction") { "1577" (make-noble "Paolo Giovanni" "1577" "faction")} (make-noble "Paolo Giovanni" "1577" "faction") {} 1 "faction" "<name=\"Paolo Giovanni [1577]\"><font size=+1><b>Paolo Giovanni [1577]</b></font>"

        ))


(deftest should-merge-nobles
    (are [expected
          existing-noble
          new-noble]

         (= expected (merge-nobles existing-noble new-noble))

         (make-noble "New" "1234" "new" (list [(list 1 12) "ax34"] [(list 2 0) "ax35"]))
         (make-noble "exi" "1234" "exi" (list [(list 1 12) "ax34"]))
         (make-noble "New" "1234" "new" (list [(list 2 0) "ax35"]))

         (make-noble "exi" "1234" "exi" (list [(list 2 2) "ax34"] [(list 1 12) "ax35"]))
         (make-noble "exi" "1234" "exi" (list [(list 2 2) "ax34"]))
         (make-noble "New" "1234" "new" (list [(list 1 12) "ax35"]))

         (make-noble "New" "1234" "new" (list [(list 2 2) "ax34"]))
         (make-noble "exi" "1234" "exi" nil)
         (make-noble "New" "1234" "new" (list [(list 2 2) "ax34"]))
         ))

(deftest should-get-noble-arrival-info
    (are [day loc-id line]
         (= (list day loc-id) (get-noble-arrival-info line))
         5 "bk46" " 5: Arrival at Whiteoak Marsh [bk46]."
         15 "p19" "15: Arrival at Millerscreek [p19]."
         16 "gh80" "16: Arrival at Tunnel [gh80]."))

(deftest should-get-gate-info
    (are [exp-gate current-loc line]
        (= exp-gate (get-gate-info current-loc line))
        (make-gate "x110" "ba40" "ak60" false) "ba40" "   Gate [x110], to Plain [ak60]"
        (make-gate "x957" "ba40" "s701" true)  "ba40" "   Gate [x957], sealed, to Ring of stones [s701]"))

(deftest should-get-gate-distance-info
    (are [exp-gate-distance current-loc line]
        (= exp-gate-distance (get-gate-distance-info current-loc line))
        (make-gate-distance "ba40" 4) "ba40" "The nearest gate is four provinces away."
        (make-gate-distance "ba40" 0) "ba40" "A gate exists somewhere in this province."
        (make-gate-distance "ba40" 11) "ba40" "The nearest gate is 11 provinces away."
        (make-gate-distance "ba40" 1) "ba40" "The nearest gate is one province away."))


(deftest should-get-explore-info
    (are [exp-explore current-loc line]
        (= exp-explore (get-explore-info current-loc line))
        (make-explore "p08" 1 false) nil "Exploration of [p08] uncovers no new features."
        (make-explore "az61" 1 false) nil "Exploration of [az61] uncovers no new features."
        (make-explore "bn66" 1 false) nil "A hidden inner location has been found in Forest [bn66]!"
        (make-explore "bn66" 1 false) nil "A hidden route has been found in Tunnel [bn66]!"
        (make-explore "ba40" 1 true) "ba40" "Nothing was found, but further exploration looks promising."
        (make-explore "ba40" 1 true) "ba40" "Rumors speak of hidden features here, but none were found."
        (make-explore "ba40" 1 true) "ba40" "We suspect something is hidden here, but did not find anything."
        (make-explore "ba40" 1 true) "ba40" "Something may be hidden here.  Further exploration is needed."))


(deftest should-merge-explores
    (are [exp-merged e1 e2]
        (= exp-merged (merge-explores e1 e2))
        (make-explore "ad23" 3 false) (make-explore "ad23" 1 false) (make-explore "ad23" 2 false)
        (make-explore "ad23" 3 true) (make-explore "ad23" 1 true) (make-explore "ad23" 2 false)
        (make-explore "ad23" 0 true) (make-explore "ad23" 0 true) (make-explore "ad23" 0 true)))

(deftest should-find-last-noble-location
    (let [a-noble (make-noble "New" "1234" "new" (list [(list 1 12) "ax35"]  [(list 1 0) "ax34"] [(list 2 15) "ax36"] [(list 3 1) "ax37"]))]
    (are [exp-loc turn noble]
        (= exp-loc (find-last-loc turn noble))
        "ax34" 0 a-noble
        "ax35" 1 a-noble
        "ax36" 2 a-noble
        "ax37" 3 a-noble)))

(deftest process-results-should-fill-noble-locations
    (let [noble1  (make-noble "New" "1234" "new" (list [(list 1 12) "ax35"] [(list 1 0) "ax34"] [(list 2 15) "ax36"] [(list 3 1) "ax37"]))
         explore1  (make-explore [1 "1234"] 3 false)
         explore2  (make-explore [2 "1234"] 3 false)
         explore3  (make-explore "ax35" 1 true)]
    (is (=
        (make-explore "ax34" 3 false)
        ((:explores (process-results { :nobles { (:id noble1) noble1 } :explores { (:location explore1) explore1} })) "ax34")))
    (is (=
        (make-explore "ax35" 4 true)
        ((:explores (process-results { :nobles { (:id noble1) noble1 } :explores { (:location explore1) explore1,
            (:location explore2) explore2,
            (:location explore3) explore3  } })) "ax35")))
    (is (=
        (make-explore "ax35" 3 false)
        ((:explores (process-results { :nobles { (:id noble1) noble1 } :explores { (:location explore2) explore2} })) "ax35")))))

(deftest should-get-market-info
    (are [exp-trade line]
        (= exp-trade (get-market-info line))
        (make-trade "buy"  "p19" "7" "17" "5" "clay pots" "95")
        "     buy    p19       7     17         5   clay pots [95] "

        (make-trade "sell" "p19" "27" "42" "43" "tea"  "w009")
        "    sell    p19      27     42        43   tea [w009]"

        (make-trade "buy" "b18" "62" "2" "1,000" "riding horses" "52")
        "     buy    b18      62      2     1,000   riding horses [52]"
         ))

(deftest should-find-province-from-inner-loc
    (let [test-loc (make-location "Plain" "aa11" "plain" #{(make-route  "k000" "In" 1 false)} )
          inner-loc (make-location "Rocky Hill" "k000" "rocky hill" #{ (make-route  "aa11" "Out" 1 false)})
          province (make-location "Plain" "aa11" "plain")
          inner-loc2 (make-location "Rocky Hill" "k000" "rocky hill")]
    ; first the simplest case where we already give a province
    (is (= "aa11" (:id (to-province province {}))))
    ; then another simple case where the "Out" direction of the inner loc is filled in
    (is (= "aa11" (:id (to-province inner-loc {(:id test-loc) test-loc}))))
    ; finally the most difficult case where we need to search in the given loc
    (is (= "aa11" (:id (to-province inner-loc2 {(:id test-loc) test-loc}))))
    ))

(deftest should-convert-between-locid-and-coordinate
    (is (= [0 11] (locid-to-co "aa11")))
    (is (= [21 43] (locid-to-co "bb43")))
    (is (= "aa11" (co-to-locid [0 11])))
    (is (= "bb43" (co-to-locid [21 43])))
    (is (= "fz99" (co-to-locid [-1 -1]))) ; make sure the map wraps around
    )

(deftest should-return-provinces-at-distance
    (are [exp-provs center dist compare-fn]
          (= exp-provs (into #{} (provinces-at-distance center dist compare-fn)))
          #{ "bb43" } "bb43" 0 =
          #{ "bb42" "bb44" "ba43" "bc43" } "bb43" 1 =
          #{ "bb41" "ba42" "az43" "ba44" "bb45" "bc44" "bd43" "bc42" } "bb43" 2 =
          #{ "aa99" "aa01" "ab00" "fz00" } "aa00" 1 =
          #{ "aa00" } "aa00" 1 <
          #{ "bb43" "bb42" "bb44" "ba43" "bc43" "bb41" "ba42" "az43" "ba44" "bb45" "bc44" "bd43" "bc42" } "bb43" 2 <=
          ))


(deftest should-make-GateHits
    (let [locs { "ab24" (make-location "Swamp" "ab24" "swamp")
                 "ab26" (make-location "Swamp" "ab26" "swamp")}]
    (are [exp-hits detections]
         (= (into #{} exp-hits) (into #{} (make-gatehits detections locs)))
         [(GateHits. "ab23" #{ "ab24" }) (GateHits. "ab25" #{ "ab24" })
          (GateHits. "aa24" #{ "ab24" }) (GateHits. "ac24" #{ "ab24" })]
         [(make-gate-distance "ab24" 1)]

         [(GateHits. "ab23" #{ "ab24" }) (GateHits. "ab25" #{ "ab24" "ab26" })
          (GateHits. "aa24" #{ "ab24" }) (GateHits. "ac24" #{ "ab24" })
          (GateHits. "aa26" #{ "ab26" }) (GateHits. "ac26" #{ "ab26" })
          (GateHits. "ab27" #{ "ab26" })]
         [(make-gate-distance "ab24" 1) (make-gate-distance "ab26" 1)]

         [(GateHits. "ab23" #{ "ab24" }) (GateHits. "ab25" #{ "ab24" })
          (GateHits. "aa24" #{ "ab24" }) (GateHits. "ac24" #{ "ab24" })]
         [(make-gate-distance "ab24" 1) (make-gate-distance "ab24" 0)]
         )))

(deftest should-get-garrison-info
  (are [exp-garr line]
    (= exp-garr (get-garrison-info line 1))
    (make-garrison "6285" "av60" "1995" #{1}) "  6285  av60   26   62   50    0   1995 2297 3527 6341"
    (make-garrison "5040" "dd88" "3090" #{1}) "  5040  dd88   10   20   50   15   3090 6372 1036 6185  ... 4437"
    (make-garrison "4937" "az61" "1995" #{1}) "  4937  az61   10   20  200    -   1995 8702 3527"
    (make-garrison "2461" "bn60" "6452" #{1}) "  2461  bn60   10   10  350    -   6452 4794 4093 9805"))



(deftest should-get-garrison-log
  (are [exp-take line]
    (= exp-take (get-garrison-log line))
    { :garrison "8455" :change (- 15) :item "11" }  "8455: Mister Fantastic [1415] took 15 workers [11] from us."
    { :garrison "6665" :change (- 1)  :item "10" }  "6665: Hodgesaaargh [2075] took one peasant [10] from us."
    { :garrison "1234" :change (- 3)  :item "271" } "1234: Captain Blutch [7612] took three centaurs [271] from us."
    { :garrison "1234" :change (- 1)  :item "d081" }"1234: Skeleton [113338] took one ancient scroll [d081] from us."
    { :garrison "2849" :change 1    :item "271" }  "2849: Received one centaur [271] from Bigstick [7568]."
    { :garrison "2481" :change 1    :item "16" }  "2481: Received one pikeman [16] from Aerys Targaryen [8702]."
    { :garrison "5310" :change 20   :item "11" } "5310: Received 20 workers [11] from Mud [2902]."
    { :garrison "4937" :change 43   :item "11" } "4937: Received 43 workers [11] from Goblin miner [1251]."
    { :garrison "8600" :change 200   :item "1" } "8600: Received 200 gold [1] from Captain Jack [3906]."
    { :garrison "1821" :change 1   :item "d081" } "1821: Received one ancient scroll [d081] from Skeleton [113338]."
    { :garrison "2461" :change 10   :item "10" } "2461: Received ten peasants [10] from Bockor [1276]."
    ))

(deftest should-update-inventory
  (let [garrisons {"1234" (make-garrison "1234" "aa00" "4321" #{1}),
                   "4567" (make-garrison "4567" "aa01" "4321" {"11" 12} #{1})}]
  (are [exp update]
    (= (assoc garrisons (:id exp) exp) (update-inventory garrisons update))
    (make-garrison "1234" "aa00" "4321" {"11" 5} #{1}) {:garrison "1234" :change 5 :item "11"}
    (make-garrison "4567" "aa01" "4321" {"11" 7} #{1}) {:garrison "4567" :change (- 5) :item "11"}
    {:id "4444", :inventory {"11" 7}} {:garrison "4444" :change 7 :item "11"}    )))

(deftest should-merge-garrisons
  (are [result garra garrb]
    (= result (merge-garrisons garra garrb))
    (make-garrison "1234" "aa00" "4321" {"11" 5, "22" 3, "71" (- 3)} #{1 2 3})
    (make-garrison "1234" "aa00" "4321" {"11" 7, "22" 3} #{1 3}) (make-garrison "1234" "aa00" "4321" {"11" (- 2), "71" (- 3)} #{2})

    (make-garrison "1234" "aa00" "4321" {"11" 7, "22" 3} #{1 2 3})
    (make-garrison "1234" "aa00" "4321" {"11" 7, "22" 3} #{1 2 3}) (make-garrison "1234" "aa00" "4321" {"11" (- 2), "71" (- 3)} #{2})

    (make-garrison "4444" "aa00" "4321" {"11" 7, "22" 3} #{2})
    {:id "4444", :inventory {"11" 7}} (make-garrison "4444" "aa00" "4321" {"22" 3} #{2})
    ))


(run-all-tests #"parse-report.analyze-test")
