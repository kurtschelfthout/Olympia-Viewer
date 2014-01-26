(ns parse-report.analyze
    (:import (java.io BufferedReader StringReader))
    (:require [clj-http.client :as client]
              [clojure.java.io :as io]
              [clojure.string :as string]
              [clojure.set :as set]
              [parse-report.factions :as factions]
              [parse-report.tables :as tables])
    (:use clojure.core))

(defn map-values [f m] (into {} (for [[k v] m] [k (f v)])))


(def base-report-url "http://www.shadowlandgames.com/olympia/turns/")
(def base-report-folder "reports\\")
(def public-report-list-url "http://www.shadowlandgames.com/olympia/public/")
(def base-public-report-url "http://www.shadowlandgames.com/olympia/public/")

(defn download-web-page
    "Downloads the webpage at the given url and returns its contents. Assumes no credentials are needed if user/password is nil."
    ([^String url] (download-web-page url nil nil))
    ([^String url ^String user ^String password]
    (:body (client/get url {:basic-auth [user password]}))))

(defn save-report
    "Downloads the given turn for the given faction with the given
	password from the given base url/turn/factionid, and saves it at
	the default location <turn>/factionid under the given folder.
	If a password is not given, it's assumed the report is a public report.
	Returns false if the report already existed, and true if it did not
	and so was downloaded."
    ([turn ^String faction-id] (save-report turn faction-id nil))
    ([turn ^String faction-id ^String password]
    (let     [url (if password (str base-report-url faction-id "/") ;prev.html") todo
                               (str base-public-report-url turn "." faction-id))
             file (io/file base-report-folder (str turn) (str faction-id ".htm"))]
    (do
		    ;(print url "\n" file "\n")
        (io/make-parents file)
        (if (.exists file)
            false
            (try (do (spit file (download-web-page url faction-id password)) true)
              (catch Exception e (do (print e) false))))))))

(defn list-available-public-reports []
    (let [page-seq (clojure.string/split-lines (download-web-page public-report-list-url))
          match (fn [line] (re-seq #".* href=\"(\d+)\.([a-z][a-z]\d).*\">" line))
         ]
     (filter not-empty (map (comp rest first match) page-seq))))

(defn save-reports
    "Downloads the turn reports for all factions up to and including the given turn that weren't already downloaded in the reports folder. Returns the number of downloaded reports."
    [until-turn]
    (let     [down-one (fn [[turn faction-id faction-password]] (if (save-report turn faction-id faction-password) 1 0))
             turns-factions (for [faction factions/factions] [until-turn (:id faction) (:password faction)])]
    (reduce + (pmap down-one (concat turns-factions
                                    (map #(concat % [nil]) (list-available-public-reports)))))))

(defn numbers [n]
    (let [words {"zero" 0, "one" 1, "two" 2,
                 "three" 3, "four" 4, "five" 5,
                 "six" 6, "seven" 7, "eight" 8, "nine" 9, "ten" 10}
         word (words n)]
    (if word word (int n))))

(defrecord Route [^String to ^String direction ^long distance hidden])
(defn make-route [^String to ^String direction ^long distance hidden] (Route. to direction distance hidden))

(defrecord Location [^String name ^String id ^String type routes visits ^String region ^String controlled-by ^String controlled-by-in])
(defn make-location
    ([^String name ^String id ^String type]
        (make-location name id type #{}))
    ([^String name ^String id ^String type routes]
        (make-location name id type routes #{}))
	([^String name ^String id ^String type routes visits]
        (make-location name id type routes visits nil))
    ([^String name ^String id ^String type routes visits ^String region]
        (make-location name id type routes visits region nil nil))
    ([^String name ^String id ^String type routes visits ^String region controlled-by controlled-by-in]
        (Location. name id type routes visits region controlled-by controlled-by-in)))

(defn add-route [^Route route ^Location loc]
    "Returns a location with the given route added to the loc."
    (assoc loc :routes (conj (:routes loc) route)))

(defn merge-locations
    "Merges the existing first loc with another one - the new-loc is only used if it is visited. Routes are merged in any case if they're not hidden."
    [^Location in-res ^Location new-loc]
	(let [last-visited (fn [vst] (if (empty? vst) 0 (apply max (map first vst))))
	      [loser winner] (if (> (last-visited (:visits new-loc)) (last-visited (:visits in-res))) ;most accurate info if we've been later
							[in-res new-loc]
							[new-loc in-res])]
    (update-in (assoc (merge loser winner) :visits (set/union (:visits in-res) (:visits new-loc)))
			   [:routes] set/union (:routes loser)))) ; need something smarter here to get rid of buildings. But location reports of garrisons are empty. Maybe should detect that and not parse those as locations.

(defn add-loc [locations ^Location loc-to-add]
    "Adds the loc-to-add if it does not exist in locations. Otherwise, merges it with
    the existing location."
    (let [id (:id loc-to-add)]
    (if (contains? locations id)
        (assoc locations id (merge-locations (locations id) loc-to-add))
        (conj locations {(:id loc-to-add) loc-to-add}))))

(defn split-line [^String line rg]
    (map #(string/trim %) (string/split line rg)))

(defn remove-day [^String line]
    "Removes the day from the front of the line if it exists; and returns the line and the day."
    (let [match (first (re-seq #"([1-3]?[0-9]): ?(.*)" line))]
    (if (empty? match)
        [line nil]
        [(nth match 2) (Integer. (nth match 1))])))

(defn parse-distance [^String distance-word]
    "Parses the word to a int. The word should be of the form <int> something, or impassable. In the latter case, -1 is returned."
    ;(do (print distance-word)
    (if (.equals "impassable" distance-word)
        -1
        (Integer. (first (split-line distance-word #" ")))))

(defn ^String remover
  "Removes any given characters from the right side of string."
  [^CharSequence s & characters]
  (loop [index (.length s)]
    (if (zero? index)
      ""
      (if (some #(= % (.charAt s (dec index))) characters)
        (recur (dec index))
        (.. s (subSequence 0 index) toString)))))

(defn parse-word-id [^String route-to]
    "Parse something of the form 'foo bar [id]' by extracting the bit before the brackets and the bit between the brackets and returning both of those as a tuple."
    (let [parts (split-line route-to #"\[")
          before (first parts)
          id (remover (last parts) \] \.)]
    [before id]))

(def location-types #{  "forest" "plain" "desert" "mountain" "swamp" "ocean"
                        "city" "port city" "tunnel" "chamber" "hades" "cloud" "underground"})

(defn guess-type-from-name [^String name]
    (let [t (.toLowerCase name)]
    (if (location-types t) t)))

(defn get-route-info [split-route]
    (let [split-route (if (.startsWith (first split-route) "To") (cons "Id" split-route) split-route) ; if we don't have a direction, put the 'explicit id' direction at the front
	      direction (first split-route)
          hidden (if (some #(= "hidden" %) split-route) true false)
          distance (parse-distance (last split-route)) ;(if hidden (last (butlast split-route)) (last split-route)))
          maybe-loc-type (nth split-route 1)
          routeIndex (if (location-types maybe-loc-type) 2 1)
          [to-name to] (parse-word-id (nth split-route routeIndex))
          name (.substring to-name 3 (.length to-name))
          type (if (location-types maybe-loc-type) maybe-loc-type (guess-type-from-name name))]
    [(make-route to direction distance hidden) (make-location name to type)]))

(defn get-inner-loc-info [[name-id type & more :as all]]
    "returns both a location and a route - location is only used if it does not exist yet"
    (let [[name id] (parse-word-id name-id)
          dist-word (first (filter #(or (.endsWith % "day") (.endsWith % "days")) more))
          distance (if dist-word (parse-distance dist-word) 0) ;0 if distance not given - is inner building
          hidden  (if (some #(= "hidden" %) more) true false)]
    [(make-location name id type) (make-route id "In" distance hidden)]))

(defn city? [^String type] (.contains type "city"))

(defn get-rumored-city-info [^String line]
    (let [[city province] (split-line line #",")
          [city-name city-id] (parse-word-id city)
          [province-to-name province-id] (parse-word-id province)
          province-name (nth (split-line province-to-name #" ") 1)
          province-type (guess-type-from-name province-name)]
    [(make-location city-name city-id "city")
     (make-location province-name province-id province-type #{ (make-route city-id "In" 1 false) })]))

(defn get-province-control-info [^String line]
    (let [match (re-matches #"^Province controlled by [^\[]+ \[([\d|\w]{4})\], castle, in [^\[]+ \[([\d|\w]{3,4})\]$" line)]
    (if match [(get match 1) (get match 2)])))


(defn get-skills [^String line]
    (let [skill-words (split-line line #",")]
    (map (fn [skill-word] (let [[_ skill-nb] (parse-word-id skill-word)] (Integer. skill-nb)))
         (filter #(and (not (string/blank? %)) (.contains % "[")) skill-words))))

(defrecord Trade [buysell who price quantity weight-per-item item-name item-id])
(defn make-trade [buysell who price quantity weight-per-item item-name item-id]
    (Trade. buysell who price quantity weight-per-item item-name item-id))

(defn get-market-info [line]
    (let [match (first (re-seq #"^    \s?(buy|sell)\s+([\d|\w]+)\s+(\d+)\s+(\d+)\s+([\d|,]+)\s+([\w|\s]+) \[([\w|\d]+)\]" line))]
    (if (not (empty? match))
        (apply make-trade (rest match)))))

(declare match-loc-header)

(defn match-trade [{ :keys [loc-in-progress line] :as acc }]
    (let [[line _] (remove-day line)
          match (get-market-info line)]
    ;(do (print match)
    (if match
        (assoc acc :loc-in-progress (assoc loc-in-progress :trades (cons match (:trades loc-in-progress)))
                   :cont match-trade)
        (assoc acc :cont match-loc-header))))

(defn match-market-report-header [{ :keys [line] :as acc}]
    (if (.contains (first (remove-day line)) "-----    ---   -----    ---     -----   ----" )
        (assoc acc :cont match-trade)
        ;acc))
        (assoc acc :cont match-market-report-header)))

 (defn indent-of [line]
    "Determines the indentation of the given line, i.e the number of spaces before finding the first
    character. Because in the turn reports own nobles are indicated by a * as the second character on
    any line, this is considered part of the indentation."
    (let [match (re-find #"^\s[\s|*]?\s*" line)]
    (if match (.length match) 0)))

(defn match-inner-loc
    [{ :keys [loc-in-progress locations line turn faction] :as acc}]
    (let [[line _] (remove-day line)]
    (cond
        (string/blank? line)
            (assoc acc :cont match-loc-header)
        (not= 3 (indent-of line))
            (assoc acc :cont match-inner-loc)
        :else
            (let [[loc route] (get-inner-loc-info (split-line line #","))]
            (assoc acc :loc-in-progress (add-route route loc-in-progress)
                        :locations      (add-loc locations (assoc loc :visits #{[turn faction]}))
                        :cont            match-inner-loc)))))

(defn match-skills-taught
    [{ :keys [loc-in-progress line] :as acc}]
    (let [[line _] (remove-day line)]
    (if (string/blank? line)
        (assoc acc :cont match-loc-header)
        (let [new-skills (get-skills line)
              skills (concat (:skills loc-in-progress) new-skills)
              new-loc-in-progress (assoc loc-in-progress :skills skills)]
        (assoc acc     :loc-in-progress    new-loc-in-progress
                    :cont                match-skills-taught)))))

(defn match-rumored-city
    [{ :keys [locations line] :as acc}]
    (let [[line _] (remove-day line)]
    (if (string/blank? line)
        (assoc acc :cont match-loc-header)
        (let [[city province] (get-rumored-city-info line)]
        (assoc acc  :locations     (add-loc (add-loc locations city) province)
                    :cont        match-rumored-city)))))

(defn match-route
    [{ :keys [loc-in-progress locations line] :as acc}]
    (let [[line _] (remove-day line)
          parts (split-line line #",")]
    ;(do (println line)
    (cond
        (string/blank? line)
            (assoc acc :cont match-loc-header)
        (not= 3 (indent-of line))
            (assoc acc :cont match-route)
        :else
            (let [[route loc] (get-route-info parts)
				  loc (if ( = "Out" (:direction route))
						(add-route  (make-route (:id loc-in-progress) "In" (:distance route) (:hidden loc-in-progress)) loc)
						loc)]
            (assoc acc  :loc-in-progress (add-route route loc-in-progress)
                        :locations       (add-loc locations loc)
                        :cont            match-route)))))

(defn remove-html [line]
    "Removes html markup from a line of text. Contents in between tags is preserved."
    (if (.startsWith line "<") ;optimization - most lines have no html
        (string/replace line, "<(.|\n)*?>" "")
        line))

(defn get-civ-info [etc]
    (if (.contains etc "wilderness")
        0
        (let [civ (second (re-matches #".*?civ-([0-9])" etc))]
        (if civ (Integer. civ)))))

(defn get-loc-info [line turn faction]
    "Gets location info from a line if it is a location header - otherwise, returns nil."
    (let [[line _] (remove-day line)
            ; a non [ and a non : (to avoid matching 'Location:' followed by a loc id. Can't start with space to avoid matching noble locations spread over two lines
          match (re-seq #"^([^\s][^\[:]+) \[([a-z][a-z]?[0-9][0-9][0-9]?)\], (.+?), in (.*)" (remove-html line))]
    (if (not (empty? match))
        (let [[_ name id type etc] (first match)
              region (first (split-line etc #","))
              loc (assoc (make-location name id type) :visits #{[turn faction]} :region region)
              hidden (.contains etc "hidden")
              loc (if hidden (assoc loc :hidden true) loc)
              civ (get-civ-info etc)
              loc (if civ (assoc loc :civ civ) loc)]
        loc))))

(defn match-nothing [acc] (assoc acc :cont match-nothing))

(defn match-loc-header
    [{ :keys [loc-in-progress locations turn faction line] :as acc}]
    "Matches one of the headers inside a location description,
    and dispatches to the right function to parse it."
    (let [  line (first (remove-day line))
            loc-info (get-loc-info line turn faction)]
    ;(do (print line)
    (cond
        loc-info
            (if loc-in-progress
                ;we've just parsed a loc and have found a new one
                (assoc acc  :loc-in-progress     loc-info
                            :locations           (conj locations {(:id loc-in-progress) loc-in-progress})
                            :cont                match-loc-header)
                ;this is the first loc we find
                (assoc acc  :loc-in-progress     loc-info
                            :cont                 match-loc-header))
        (and (nil? loc-in-progress) (nil? loc-info))
                ; haven't found a location yet - keep looking
                (assoc acc :cont match-loc-header)
		(or (.startsWith line "It is windy.")
		    (.startsWith line "It is raining.")
			  (.startsWith line "The province is blanketed in fog.")
		    (.startsWith line "Seen here"))
			; we are done with the current loc - need this to fix a bug
			; e.g. vision loc, then vision ship. The ship is not recognized as a location,
			; but parsing continues so the routes "Out" of the ship is added to the location.
			(assoc acc  :locations (conj locations {(:id loc-in-progress) loc-in-progress})
                        :loc-in-progress nil
                        :cont      match-loc-header)
        (.startsWith line "Province controlled by")
            (let [[by in] (get-province-control-info line)]
            (assoc acc :loc-in-progress (assoc loc-in-progress :controlled-by by :controlled-by-in in)
                       :cont match-loc-header))
        (.startsWith line "Routes leaving")
            (assoc acc :cont match-route)
        (.startsWith line "Cities rumored to be nearby:")
            (assoc acc :cont match-rumored-city)
        (.startsWith line "Skills taught here:")
            (assoc acc :cont match-skills-taught)
        (.startsWith line "Inner locations:")
            (assoc acc :cont match-inner-loc)
        (.startsWith line "Market report:")
            (assoc acc :cont match-market-report-header)
        (and (let [l (remove-html line)] (or (.startsWith l "Order template")
                                             (.startsWith l "Lore sheet")
                                             (.startsWith l "New players")))
             loc-in-progress)
            ;we've just parsed a location and are now done
                (assoc acc  :locations (conj locations {(:id loc-in-progress) loc-in-progress})
                            :cont      match-nothing)
        :else
            (assoc acc :cont match-loc-header))))

(defrecord Noble [name id faction locations])
(defn make-noble
  ([name id faction] (Noble. name id faction (list)))
  ([name id faction locations] (Noble. name id faction locations)))

(defn add-new-noble [{ :keys [id] :as noble} nobles]
	(if (nobles id) nobles (assoc nobles id noble)))

(defn add-new-noble-location [noble turn location-id]
    (let [new-locs (conj (:locations noble) [turn location-id])]
    (assoc noble :locations new-locs)))

(defn merge-nobles [existing-noble new-noble]
    (let [latest-turn (fn [n] (apply max (cons 0 (map (comp first first) (:locations n)))))]
    ;(do (print (str existing-noble " " new-noble))
    (if (> (latest-turn new-noble) (latest-turn existing-noble))
        (make-noble (:name new-noble) (:id new-noble) (:faction new-noble)
            (concat (:locations existing-noble) (:locations new-noble)))
        (make-noble (:name existing-noble) (:id existing-noble) (:faction existing-noble)
            (concat (:locations existing-noble) (:locations new-noble))))))


(defrecord Gate [id from to sealed])
(defn make-gate [id from to sealed]
    (Gate. id from to sealed))

;; don't forget to call remove-html and remove-day before calling this
(defn get-gate-info [current-loc line]
    "Parses something of the form '   Gate [x957], sealed, to Ring of stones [s701]' and returns
    a new gate record from that, or nil if it isn't a match."
    (let [match (first (re-seq #"^   [^\[]+ \[([^\]]{4})\], (sealed, )?to [^\[]+ \[([^\]]{4})\]" line))]
    ;(do (print line)
    (if (not (empty? match))
        (make-gate (nth match 1) current-loc (nth match 3) (not (string/blank? (nth match 2)))))))

(defrecord GateDistance [location distance])
(defn make-gate-distance [location distance]
  (GateDistance. location distance))

(defn get-gate-distance-info [current-loc line]
        "Parses something of the for 'A gate exists somewhere in this province.' or
        'The nearest gate is one province away.' and returns a new gate-distance record from that."
        (if (= line "A gate exists somewhere in this province.")
            (make-gate-distance current-loc 0)
            (let [match (first (re-seq #"^The nearest gate is (\w*|\d\d) province[s]? away." line))]
            (if (not (empty? match))
                (make-gate-distance current-loc (numbers (nth match 1)))))))

; Counts how many times the given location has been "hit" by a gate detection - that is, how many
; times it has been in range of a different gate detection.
(defrecord GateHits [location origins])

(defn province? [loc]
    (#{"forest" "plain" "desert" "mountain" "swamp" "ocean"} (:type loc) ))

(defn to-province [loc locs]
;    not tail recursive in clojure, but should be just a couple of calls.
    (let [find-first (fn [f coll] (first (filter f coll)))]
    (if (province? loc)
        loc
        (let [out (locs (:to (find-first #(= "Out" (:direction %1)) (:routes loc))))
              has-route (fn [l] (find-first (fn [r] (and (= (:id loc) (:to r)) (= "In" (:direction r)))) (:routes l)))
              out (if out out (find-first has-route (vals locs)))]
        (if out (to-province out locs))))))



(defn locid-to-co [loc-id]
    "given a location id on Provinia (e.g. aa11), gives back a coordinate that can be used to count with (e.g. [1 1])"
    (let [letters (filter (complement #{\e \i \l \o \u \y }) "abcdefghijklmnopqrstuvwxyz")
          letter-to-co (fn [s] (if-let [found ((zipmap letters (range 26)) s)] found 0)) ;non-provinia locations...not handled. Return 0.
          base (count letters)
          [l1 l2] (take 2 loc-id)
          result (+ (* base (letter-to-co l1)) (letter-to-co l2))]
    (if (nil? loc-id) [0 0] [result (Integer. (.substring loc-id 2 3))])))

(defn co-to-locid [[x y]]
    "Reverse of locid-to-co"
    (let [[x y] [(mod x 100) (mod y 100)]
          letters (filter (complement #{\e \i \l \o \u \y }) (map char (range \a (inc \z))))
          co-to-letter (zipmap (range 100) (for [i (take 6 letters) j letters] (apply str (list i j))))]
    (apply str (list (co-to-letter x) (format "%02" y)))))

(defn provinces-at-distance [locid dist compare-fn]
    (let [[x y :as c] (locid-to-co locid)
          manhattan-dist (fn [[x1 y1] [x2 y2]] (+ (Math/abs (- x1 x2)) (Math/abs (- y1 y2))))]
    (for [rx (range (- x dist) (inc (+ x dist))) ry (range (- y dist) (inc (+ y dist)))
          :when (compare-fn (manhattan-dist c [rx ry]) dist)]
          (co-to-locid [rx ry]))))

(defn make-gatehits [detections locs]
    (let [no-gates (into #{} (mapcat #(let [prov (:id (to-province (locs (:location %)) locs))] (provinces-at-distance prov (:distance %) <)) detections))
          r1 (map #(let [prov (:id (to-province (locs (:location %)) locs))] [prov (provinces-at-distance prov (:distance %) =)]) detections)
          r2 (mapcat (fn [[origin provs]] (map (fn [p] [p origin]) provs)) r1)
          r3 (group-by first r2)
          r4 (map-values (fn [v] (map #(first (rest %)) v)) r3)
          r5 (remove (fn [[k v]] (no-gates k)) r4)]
        (map (fn [[prov origins]] (GateHits. prov (into #{} origins))) r5)))

; location id, number of failed explores, true if something is hidden ("further exploration looks promising")
(defrecord Explore [location failures hidden])
(defn make-explore [location failures hidden] (Explore. location failures hidden))

(defn merge-explores
    [ { loc1 :location fail1 :failures hid1 :hidden }
    { fail2 :failures hid2 :hidden }]
    "Merges two locations. Pre: the id of both is the same."
    (make-explore loc1 (+ fail1 fail2) (or hid1 hid2)))


(defn get-explore-info [current-loc line]
    "Parses something of the form 'Exploration of [p08] uncovers no new features.' or 'further exploration
    looks promising' and return a new explore record."
    (let [match-failure (first (re-seq #"^Exploration of \[([\w|\d]+)\] uncovers no new features." line))
          match-success (first (re-seq #"^A hidden .*? has been found in .*? \[([\w|\d]+)\]!" line))
          match-hidden (#{ "Nothing was found, but further exploration looks promising."
                           "Rumors speak of hidden features here, but none were found."
                           "We suspect something is hidden here, but did not find anything."
                           "Something may be hidden here.  Further exploration is needed." } line)]
          (cond
            (not (empty? match-failure))
                (make-explore (nth match-failure 1) 1 false)
            (not (empty? match-success))
                (make-explore (nth match-success 1) 1 false)
            match-hidden
                (make-explore current-loc 1 true))))

(declare match-garrisons-then-noble-header)

(defn get-noble-location-info [line]
    ;   between brackets: anything that is NOT ] repeated at least 3-5 times
    (let [match (re-seq #"^   Location:       [^\[]+ \[([^\]]{3,5})\], in .*" (remove-html line))]
    (if (not (empty? match))
        (nth (first match) 1))))

(defn get-noble-arrival-info [line]
    "Parses something like '15: Arrival at Whiteoak Marsh [bk46].' and returns the day and the location id."
    (let [match (first (re-seq #"^([\d|\s]\d): Arrival at [^\[]+ \[([^\]]{3,5})\]" (remove-html line)))]
    (if (not (empty? match))
        (list (Integer. (nth match 1)) (nth match 2)))))

(defn match-noble-info
    [{ :keys [nobles noble-in-progress turn current-loc explores gates gate-distances line] :as acc}] ;add have-arrival to args
    (let [is-prisoner (.contains line "is being held prisoner")
          has-died (.endsWith line "has died ***")
          arrival (get-noble-arrival-info line)
          location (get-noble-location-info line)
          [clean-line _] (remove-day (remove-html line))
          explore (get-explore-info current-loc clean-line)
          gate-info (get-gate-info current-loc clean-line)
          gate-dist (get-gate-distance-info current-loc clean-line)]
    (cond
        is-prisoner
            (assoc acc :cont match-garrisons-then-noble-header)
        has-died
            (assoc acc :cont match-garrisons-then-noble-header)
        location
            (assoc acc :noble-in-progress (add-new-noble-location noble-in-progress (list turn 30) location)   ;(if have-arrival noble-in-progress (add-new-noble-location noble-in-progress (list turn 30) location))
                       :current-loc location
                       :cont match-garrisons-then-noble-header)
                       ;:have-arrival false)
        arrival
            (let [[day loc-id] arrival]
            (assoc acc :noble-in-progress (add-new-noble-location noble-in-progress (list turn day) loc-id)
                       :current-loc loc-id
                       :cont match-noble-info))
                      ; :have-arrival true))
        explore
            (assoc acc :explores (merge-with merge-explores { (:location explore) explore } explores)
                       :cont match-noble-info)
        gate-info
            ; gates are static so don't need merging - we can just overwrite
            ;(do (print gate-info)
            (assoc acc :gates (assoc gates (:id gate-info) gate-info)
                       :cont match-noble-info)
        gate-dist
            ;(do (print gate-dist)
            (assoc acc :gate-distances (assoc gate-distances (:location gate-dist) gate-dist)
                       :cont match-noble-info)
        :else
            (assoc acc :cont match-noble-info))))

(defrecord Garrison [id location-id castle-id inventory seen])

(defn make-garrison
  ([id location-id castle-id seen]
    (make-garrison id location-id castle-id {} seen))
  ([id location-id castle-id inventory seen]
    (Garrison. id location-id castle-id inventory seen)))

(defn update-inventory [garrisons {:keys [garrison change item]}]
  "We can assume here the garrison is already known, since we've parsed the garrison overview before."
  (let [with-garrison (update-in garrisons [garrison :id] (fn [old] garrison))]
  (update-in with-garrison [garrison :inventory item] (fn [old ch] (if old (+ old ch) ch)) change)))

(defn merge-garrisons [{seena :seen :as garrisona} {seenb :seen :as garrisonb}]
  ; in general, two garrisons can no be merged this way. But because we know that one of the two garrisons is only going to have the inventory of one
  ; turn, the following works.
  (if (empty? (set/intersection seena seenb))
    (let [base-garrison (if (:location-id garrisona) garrisona garrisonb)] ;there is a rare case where a gar is found in the log but not in the overview. In that case, the location-id will be zero, and we want to merge from the other one.
    (assoc base-garrison :inventory (merge-with + (:inventory garrisona) (:inventory garrisonb))
                         :seen (set/union seena seenb)))
    (if (>= (count seena) (count seenb)) garrisona garrisonb)))


(defn get-garrison-info [line turn]
  (when-let [[[_ garrison where castle]] (re-seq #"^\s+(\S+)\s+(\S+)\s+(?>\d+)\s+(?>\d+)\s+(?>\d+)\s+(?>\d+|-)\s+(\S+)\s.*" line)]
    (make-garrison garrison where castle #{turn})))

(defn get-garrison-log [line]
 (if-let [[[_ garrison nb item]] (re-seq #"^([\d\w]{4}): .*? took (\w*|\d*) [^\[]*\[([^\]]{1,4})\] from us\." line)]
   {:garrison garrison :change (- (if-let [number (numbers nb)] number (long nb))) :item item }
   (when-let [[[_ garrison nb item]] (re-seq #"^([\d\w]{4}): Received (\w*|\d*) [^\[]*\[([^\]]{1,4})\] from .*" line)]
     {:garrison garrison :change (if-let [number (numbers nb)] number (long nb)) :item item })))

(declare match-garrisons-then-noble-header)

(defn match-garrison-log
  [{:keys [line garrisons skip] :as acc}]
  ;(do (println line)
  (if (= 1 skip)
    (assoc acc :cont match-garrison-log :skip 0)
    (if-let [change (get-garrison-log (nth (remove-day line) 0))]
      ;(do (println change)
      (assoc acc :garrisons (update-inventory garrisons change)
                 :cont match-garrison-log)
      (if (.contains line "Paid maintenance of")
        (assoc acc :cont match-garrisons-then-noble-header)
        (assoc acc :cont match-garrison-log)))))

(defn match-garrison-log-header
  [{:keys [line] :as acc}]
  (if (.contains line "Garrison log")
    (assoc acc :cont match-garrison-log :skip 1)
    (assoc acc :cont match-garrison-log-header)))

(defn match-garrison
  [{:keys [line garrisons turn] :as acc}]
  (if-let [new-garrison (get-garrison-info (nth (remove-day line) 0) turn)]
    (assoc acc :garrisons (assoc garrisons (:id new-garrison) new-garrison)
               :cont match-garrison)
    (assoc acc  :cont match-garrison-log-header)))

(defn get-noble-header [line]
  (re-seq #"^(\w[\w|\s]*) \[(\d|\w\d\d\d)\]$" (remove-html line)))

(defn match-garrisons-then-noble-header
    [{ :keys [nobles noble-in-progress turn faction line] :as acc}]
    (let [match (get-noble-header line)]
    ;(do (print line "\n")
    (cond
      (.contains line "---- -----  --- ----  --- ---- ------ ------")
        (assoc acc :cont match-garrison)
      (not (empty? match))
          (let [[_ name id] (first match)]
          (if noble-in-progress
              (assoc acc :nobles              (add-new-noble noble-in-progress nobles)
                          :noble-in-progress   (make-noble name id faction)
                          :current-loc         [turn id] ; if the current-loc is an int, that means the loc at the beginning of the turn (which we can only determine from the previous reports, and so will have to fill in in a post processing step
                          :cont                match-noble-info)
              (assoc acc :noble-in-progress   (make-noble name id faction)
                          :current-loc         [turn id]
                          :cont                match-noble-info)))
      (and (empty? match) (nil? noble-in-progress))
          (assoc acc :cont match-garrisons-then-noble-header)
      (get-loc-info line turn faction)
          (assoc acc :nobles (add-new-noble noble-in-progress nobles)
                      :cont    match-nothing) ;rest matched in parallel by locations matcher
      :else
          (assoc acc :cont match-garrisons-then-noble-header))))

(defn match-two [{[k1 k2] :subcont line :line :as acc }]
    "Executes two matchers at the same time, passing the accumulator first to k1 then k2.
    Continuation is always match-two again."
   ; (do (print line)
    (let [{k1res :cont :as acc-1} (k1 acc)
          {k2res :cont :as acc-2} (k2 acc-1)]
    (assoc acc-2 :cont match-two
                 :subcont [k1res k2res])))



(defn match-turn-and-faction
    [{ :keys [line] :as acc}]
    (let [line (remove-html line)
          turn "Olympia G4 turn "
          len-turn (.length turn)
          faction "Report for "
          len-faction (.length faction)
          init-faction "Initial Position Report for "
          len-init-faction (.length init-faction)]
    (cond
        (.startsWith line turn)
            (assoc acc :turn (Integer. (.substring line len-turn))
                        :cont match-turn-and-faction)
        (.startsWith line faction)
            (assoc acc :faction (nth (parse-word-id (.substring line len-faction)) 1)
                       :cont match-two
                       :subcont [match-garrisons-then-noble-header match-loc-header])
        (.startsWith line init-faction)
            (assoc acc :faction (nth (parse-word-id (.substring line len-init-faction)) 1)
                       :cont match-two
                       :subcont [match-garrisons-then-noble-header match-loc-header])
       :else
            (assoc acc :cont match-turn-and-faction))))

(defn parse-report
    "Parses a turn report based on a sequence of lines."
    [lines]
    (let [loc-reduce (fn [{k :cont :as acc} line] (k (assoc acc :line line)))
          result (reduce loc-reduce {:cont match-turn-and-faction
                                     :locations {}
                                     :nobles {}
                                     :explores {}
                                     :gates {}
                                     :gate-distances {}
                                     :garrisons {}} lines)]
      (select-keys result [:locations :nobles :explores :gates :gate-distances :garrisons])))

(defn read-report
    "Returns a report as a sequence of lines."
    [turn filename]
    (let [file (io/file base-report-folder (str turn) filename)]
    (do (println "reading report: " turn filename)
        (line-seq (java.io.BufferedReader. file)))))

(defn all-reports
    "Returns a vector of turn and filename pairs that represent all the currently downloaded reports."
    []
    (for [turn (.listFiles (io/file base-report-folder))
          filename (.listFiles (io/file turn))]
          [(.getName (io/file turn)) (.getName (io/file filename))]))

(defn find-last-loc [turn noble]
    "Find the last location of the given noble at the given turn."
    (let [locs (:locations noble)
          locs-at-turn (filter (fn [[[t _] _]] (= t (max 1 turn))) locs)
          [[_ _] loc]  (if (= 0 turn) (last locs-at-turn) (first locs-at-turn))]
    loc))


(defn process-results [ {:keys [explores nobles gates gate-distances locations garrisons] :as result}]
    "Post process the results of parsing all reports. Currently, fills in the locations
    of nobles in the explored locations list if that location can only be determined from
    the previous turn report."
    (let [explore-map
            (fn [keyname all]
                (let [loc (keyname all)]
                (if (vector? loc)
                    (let [[turn noble-id] loc]
                         ;(do (print loc)
                         (assoc all keyname (find-last-loc (dec turn) (nobles noble-id))))
                    all)))
          processed-explores (map (partial explore-map :location) (vals explores))
          final-explores (reduce #(merge-with merge-explores %1 { (:location %2) %2 }) {} processed-explores)
          final-gates (reduce #(merge %1 { (:id %2) %2 }) {} (map (partial explore-map :from) (vals gates)))
          final-gate-distances (reduce #(merge %1 { (:location %2) %2 }) {} (map (partial explore-map :location) (vals gate-distances)))]
    ;(do (print explores "\n" processed-explores "\n" final-explores)
    (assoc result
           :explores final-explores
           :gates final-gates
           :gate-distances final-gate-distances
           :gate-hits (make-gatehits (vals final-gate-distances) locations))))
           ;:garrisons (map (fn [garr] (update-in garr [:inventory] into [])) garrisons))))


(defn parse-reports
    "Parses all downloaded reports into a data structure containing useful info."
    []
    (let [merge-all (fn [{loca :locations, noba :nobles, expa :explores, ga :gates, gad :gate-distances, garsa :garrisons}
                         {locb :locations, nobb :nobles, expb :explores, gb :gates, gbd :gate-distances, garsb :garrisons}]
                { :locations   (merge-with merge-locations loca locb)
                  :nobles      (merge-with merge-nobles noba nobb)
                  :explores    (merge-with merge-explores expa expb)
                  :gates       (merge   ga gb)
                  :gate-distances (merge gad gbd)
                  :garrisons  (merge-with merge-garrisons garsa garsb)})]
    (->> (all-reports)
         (map (comp parse-report #(apply read-report %)))
         (reduce merge-all)
         (process-results))))



; (to-xml {:root {:a "a" :b "b"}})
; (to-xml {:root {:a "a" :b "b" :routes {:to "k32" :id 22}}})
; (to-xml {:root {:a "a" :b "b" :routes (list "k32" "j342")}})
; (to-xml {:root {:a "a" :b "b" :routes (list {:to "k32" :dist 2} {:to "j342" :dist 3})}})
(comment
(defn to-xml
    "Parses a nested map { :elements { :a val :b val ... }} and
    writes an xml document from it using the System.Xml.Linq API.
    A nested sequence need not be a seq of pairs - in that case, each
    element in it will have the tag of the surrounding keyword
    with -elem appended."
    ([element-map]
    (let [as-tags? (fn [coll] (or (sequential? coll) (set? coll) (and (map? coll) (not (symbol? (first (first coll))) ))))
          xname (fn [st] (XName/Get (name st)))
          name-elem (fn [st] (remover (name st) \s))]
 ;   (do (println (str "map"))
    (if (map? element-map)
        (map
          (fn [[tag c]] (XElement. (xname tag) (if (as-tags? c)
                                                 (to-xml (name-elem tag) c)
                                                 (to-xml c))))
          element-map)
        (str element-map))))
    ([tag elements]
  ;  (do (println (str "list" tag element-list))
    (map (fn [c] (if (vector? c)
                   (XElement. (XName/Get tag) (cons (XAttribute. (XName/Get "key") (first c)) (to-xml (nth c 1))))
                   (XElement. (XName/Get tag) (to-xml c))))
           elements)))

;(to-xml-file {:root {:a "a" :b "b" :routes {:to "k32" :id 22}}})
(defn to-xml-file
    [filename element-map]
    (let [result (into-array (to-xml element-map))]
    (.Save (XDocument. result) filename)))


;(def base-result-folder "C:\\Users\\Kurt\\Projects\\oly\\OlyViewer\\WpfOly\\bin\\Debug\\")
(def base-result-folder ".")

(defn make-reports-xml
    []
    (let [result (parse-reports)
          filename (fn [name] (io/file base-result-folder (str name ".xml")))
          save-xml (fn [keyname]
                        (to-xml-file (filename (name keyname)) { keyname (vals (result keyname))}))]
    (doall
    [ ;(print (result :gate-hits))
     (save-xml :garrisons)
     (to-xml-file (filename "gate-hits") { :gate-hits (result :gate-hits) })
     (save-xml :locations)
     (save-xml  :nobles)
     (save-xml  :explores)
     (save-xml  :gates)
     (save-xml  :gate-distances)
    ])))

 (defn make-tables-xml
    []
    (doall
    [(to-xml-file "skills.xml" {:skills tables/skills})
    (to-xml-file "quests.xml" {:quests tables/quests})
    (to-xml-file "creatures.xml" {:creatures tables/creatures})
    (to-xml-file "innerlocs.xml" {:innerlocs tables/innerlocs})
    (to-xml-file "productions.xml" {:productions tables/productions})
    (to-xml-file "items.xml" {:items tables/items})]
    ))
)
